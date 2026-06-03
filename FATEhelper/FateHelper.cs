using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace FATEhelper;

// Dalamud's IFate doesn't have all the necessary data for this, making my own reference to use the Structs directly
internal unsafe class Fate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Fate"/> class.
    /// </summary>
    /// <param name="address">The address of this fate in memory.</param>
    internal Fate(IntPtr address)
    {
        this.Address = address;
    }

    public IntPtr Address { get; }
    public FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext*)this.Address;
    public FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext.FateObjective* ObjStruct => (FFXIVClientStructs.FFXIV.Client.Game.Fate.FateContext.FateObjective*)this.Address;
}

internal class FateInfo(Configuration cfg)
{
    public Utf8String Name { get; set; }
    public long TimeRemaining { get; set; }
    public bool NotStarted { get; set; }
    public Vector3 Position { get; set; }
    public float Distance { get; set; }
    public byte Progress { get; set; }
    public uint IconId { get; set; }
    public bool IsBonus { get; set; }
    public int CollectCount { get; set; }
    public long TimeSortBy { get; set; }
    public int ProgressSortBy { get; set; }
    public double Compass { get; set; }
    public int Level { get; set; }
    public uint ID { get; set; }

    /*
     * This returns the proper icon ID if it is an NPC-started fate
     * It uses the FateObjective Flags value since that was all I could find that worked
     * All the other FATE data for icons seems to be blank, doesn't change, or has the same value as the regular icon
     * I might be reading MapMarkerData completely wrong but none of the info there appeared to work either
     */
    public uint GetProperIcon(uint icon, uint flag, int start)
    {
        // NPC exclamation marker. 524736 for position outside of fate radius, 655809 inside radius
        // this flag DOES NOT change if someone else starts the fate, so also verify it's not started
        if (!cfg.ShowObjectiveIcon && start == 0 && (flag == 524736 || flag == 655809))
        {
            return 60458;
        }
        return icon;
    }
}

internal unsafe class FateHelper
{
    private Plugin Plugin;
    private Configuration Config;
    private Dictionary<ushort,long> NotStarted;
    public static string FateCurrency = "";
    public static byte GrandCompany;
    private int FateCount;
    private bool ForlornActive;

    public FateHelper(Plugin plugin)
    {
        Plugin = plugin;
        Config = Plugin.Configuration;
        NotStarted = new Dictionary<ushort, long>();
        ForlornActive = false;
    }
   
    public List<FateInfo> GetFateInfo()
    {
        // update count for every refresh
        FateCount = Plugin.FateTable.Length;
        if (Plugin.Configuration.ShowCurrency)
        {
            var Inventory = InventoryManager.Instance();
            // pre vs post shadowbringers (best expansion fight me), lakeland is first with 813 id
            if (Plugin.ClientState.TerritoryType < 813)
            {
                var player = PlayerState.Instance();
                if (player != null)
                {
                    GrandCompany = player->GrandCompany;
                    FateCurrency = Inventory->GetCompanySeals(GrandCompany) + " / " +
                                   Inventory->GetMaxCompanySeals(GrandCompany);
                }
            }
            else
            {
                FateCurrency = Inventory->GetInventoryItemCount(26807) + " / 1500";
            }
        }
        var fatelist = new List<FateInfo>();
        ushort[] ids =  new ushort[FateCount];
        for (var i = 0; i < FateCount; i++)
        {
            var addr = Plugin.FateTable.GetFateAddress(i);
            var info = new FateInfo(Config);
            if (addr != IntPtr.Zero)
            {
                try
                {
                    var fate = new Fate(addr);
                    var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                    var id = fate.Struct->FateId;
                    ids[i] = id;
                    int start = fate.Struct->StartTimeEpoch;
                    if (start == 0)
                    {
                        info.NotStarted = true;
                        if (!NotStarted.ContainsKey(id))
                            NotStarted.Add(id, now);
                        info.TimeRemaining = now - NotStarted[id];
                    }
                    else
                    {
                        info.TimeRemaining = start + fate.Struct->Duration - now;
                        NotStarted.Remove(id);
                    }

                    info.Name = fate.Struct->Name;
                    info.Position = fate.Struct->Location;
                    info.Progress = fate.Struct->Progress;
                    info.IsBonus = fate.Struct->IsBonus;
                    info.Level = Convert.ToInt32(fate.Struct->Level);
                    info.IconId = info.GetProperIcon(fate.Struct->MapIconId,fate.ObjStruct->Flags,start);
                    info.ID = id;
                    // player turn in, only show the number if they haven't handed in enough items
                    if (fate.Struct->HandInCount >= 6)
                        info.CollectCount = 9999;
                    else if (fate.Struct->EventItem != 0)
                        info.CollectCount = InventoryManager.Instance()->GetInventoryItemCount(fate.Struct->EventItem);
                    // set a sort by for time remaining. NPC and preparing FATEs have no start time so set it high for those
                    // if bonus sort is picked then make it largely negative to show first
                    info.TimeSortBy = (start == 0 ? (999999 - info.TimeRemaining) : info.TimeRemaining);
                    if (Config.SortBy == 0 && info.IsBonus)
                    {
                        info.TimeSortBy -= 999999999;
                    }
                    // set sort by for progress with bonus first, add if bonus since progress sorts descending
                    info.ProgressSortBy = info.Progress;
                    if (Config.SortBy == 2 && info.IsBonus)
                    {
                        info.ProgressSortBy += 200;
                    }

                    if (Plugin.ObjectTable.LocalPlayer != null)
                    {
                        // set distance from player
                        if (Config.ShowDistance || Config.SortBy == 6)
                            info.Distance = Vector2.Distance(
                                new Vector2(Plugin.ObjectTable.LocalPlayer.Position.X,
                                            Plugin.ObjectTable.LocalPlayer.Position.Z),
                                new Vector2(info.Position.X, info.Position.Z)
                            );

                        // set compass rotation angle
                        if (Config.ShowCompass)
                        {
                            var angle = Plugin.ObjectTable.LocalPlayer.Rotation - Math.Atan2(
                                            Plugin.ObjectTable.LocalPlayer.Position.X - info.Position.X,
                                            Plugin.ObjectTable.LocalPlayer.Position.Z - info.Position.Z);
                            // not sure why it is off by 45 degrees
                            info.Compass = angle + (Math.PI / 4);
                        }
                    }
                }
                catch (Exception e)
                {
                    info.Name = new Utf8String(e.Message);
                }
            }
            else
            {
                info.Name = new Utf8String("error accessing fate address");
            }
            fatelist.Add(info);
        }
        // clear out any fate IDs in NotStarted but not in current data
        foreach (var id in NotStarted.Keys)
        {
            if (Array.IndexOf(ids, id) == -1)
            {
                NotStarted.Remove(id);
            }
        }
        // alert when a forlorn spawns
        if (Config.ForlornAlert)
        {
            var objs = Plugin.ObjectTable;
            foreach (var obj in objs)
            {
                if (obj.BaseId == 7586 || obj.BaseId == 7587)
                {
                    if (!obj.IsDead)
                    {
                        if (!ForlornActive)
                        {
                            ForlornActive = true;
                            UIGlobals.PlayChatSoundEffect(9);
                        }
                    }
                    else
                    {
                        ForlornActive = false;
                    }
                }
            }
        }
        // sort list by configuration option
        List<FateInfo> sortlist = Config.SortBy switch
        {
            2 or 3 => fatelist.OrderByDescending(o => o.ProgressSortBy).ToList(),
            4 => fatelist.OrderByDescending(o => o.Level).ToList(),
            5 => fatelist.OrderBy(o => o.Level).ToList(),
            6 => fatelist.OrderBy(o => o.Distance).ToList(),
            _ => fatelist.OrderBy(o => o.TimeSortBy).ToList()
        };

        return sortlist;
    }
}

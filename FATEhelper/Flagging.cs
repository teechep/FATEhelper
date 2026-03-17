using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace FATEhelper;

// most of this code is from GatherBuddy and TeleporterPlugin, thank you/sorry for using it
internal unsafe class Flagging
{
    private readonly Configuration config;
    private readonly AgentMap* agentMap;
    private readonly Telepo* teleport;
    private readonly uint territoryId;
    private string closestAetheryte;
    private uint closestAetheryteId;
    private uint fateId;
    private Vector3 fateLocation;
    private Vector3 playerLocation;
    public Flagging(Plugin plugin, uint FateId, Vector3 FateLocation, Vector3 PlayerLocation)
    {
        config = plugin.Configuration;
        agentMap = AgentMap.Instance();
        teleport = Telepo.Instance();
        teleport->UpdateAetheryteList();
        territoryId = agentMap->CurrentTerritoryId;
        closestAetheryte = string.Empty;
        closestAetheryteId = 0;
        fateId = FateId;
        fateLocation = FateLocation;
        playerLocation = PlayerLocation;
        if (Plugin.ClientState.TerritoryType == 1252)
            GetClosestOccult();
        else
            GetClosestAetheryte();
    }
    
    private static float SquaredDistance(float x1, float y1, float x2, float y2)
    {
        x1 -= x2;
        y1 -= y2;
        return x1 * x1 + y1 * y1;
    }

    private void GetClosestAetheryte()
    {
        // aetheryte location data doesn't have a vertical coordinate
        // may cause a false positive in places like Yak T'el, but probably very few fringe cases
        float shortestDistance = SquaredDistance(playerLocation.X, playerLocation.Z, fateLocation.X, fateLocation.Z);
        var sheet = Plugin.DataManager.GetExcelSheet<Aetheryte>(Plugin.ClientState.ClientLanguage);
        var telelist = teleport->TeleportList;
        foreach (var row in sheet)
        {
            if (row.Territory.RowId == Plugin.ClientState.TerritoryType && row.IsAetheryte)
            {
                var marker = Plugin.DataManager.GetSubrowExcelSheet<MapMarker>().SelectMany(m => m).Cast<MapMarker?>().FirstOrDefault(m => m!.Value.DataType == 3 && m.Value.DataKey.RowId == row.RowId);
                if (marker != null)
                {
                    // marker values add 1024 to be always positive, so subtract to align them with other location vectors
                    var distance = SquaredDistance((marker.Value.X - 1024), (marker.Value.Y - 1024), fateLocation.X,fateLocation.Z);
                    // account for distance lost to teleporting
                    // player can move (fly) about 10000 in the 5 seconds it takes to cast teleport, 4000 for loading time and re-orientation
                    if (distance + 14000 < shortestDistance)
                    {
                        // check that player can teleport there
                        foreach (var tele in telelist)
                        {
                            if (tele.AetheryteId == row.RowId)
                            {
                                closestAetheryte = row.PlaceName.Value.Name.ToString();
                                closestAetheryteId = row.RowId;
                                shortestDistance = distance;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    
    public void FlagFate()
    {
        if (agentMap == null) 
            return;
        var mapid = agentMap->CurrentMapId;
        agentMap->SetFlagMapMarker(territoryId,mapid,fateLocation);
        if(config.OpenMapWithFlag)
            agentMap->OpenMapByMapId(mapid);
        if (config.TeleportWithFlag && closestAetheryteId != 0)
            teleport->Teleport(closestAetheryteId, 0);
    }

    public string GetClosestName()
    {
        return closestAetheryte;
    }

    // don't know if there's a way to get the list of aetheryte shards, please lmk if there is
    public void GetClosestOccult()
    {
        List<Vector3> Shards = new List<Vector3>
        {
            new Vector3((float)830.69, 0, (float)-695.86),
            new Vector3((float)-171.34,0,(float)-612.4),
            new Vector3((float)-357.95,0,(float)-120.94),
            new Vector3((float)306.98,0,(float)305.65),
            new Vector3((float)-384.15,0,(float)281.54)
        };
        int shard = 1;
        string closest = "";
        float closestDistance = SquaredDistance(playerLocation.X, playerLocation.Z, fateLocation.X, fateLocation.Z);
        // manual correction for Brain Drain
        if (fateId == 1967)
        {
            closestAetheryte = "3";
        }
        else{
            foreach (var s in Shards)
            {
                float closer = SquaredDistance(s.X, s.Z, fateLocation.X, fateLocation.Z);
                // 12000 for the 3 seconds to do occult return and running to the base aetheryte
                if (closer + 12000 < closestDistance)
                {
                    closestDistance = closer;
                    closest = shard.ToString();
                }

                shard++;
            }
            closestAetheryte = closest;
        }
    }
}

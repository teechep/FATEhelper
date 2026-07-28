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
        if (Plugin.ClientState.TerritoryType == 1346)
        {
            List<Vector3> NorthHorn = new List<Vector3>
            {
                new Vector3(879f, 0, 879f),
                new Vector3(451f, 0, 528f),
                new Vector3(357f, 0, -554f),
                new Vector3(-547f, 0, 593f),
                new Vector3(-388f, 0, -440f),
                new Vector3(-13f, 0, -40f)
            };
            GetClosestOccult(NorthHorn);
        }
        else if (Plugin.ClientState.TerritoryType == 1252)
        {
            List<Vector3> SouthHorn = new List<Vector3>
            {
                new Vector3(830.69f, 0, -695.86f),
                new Vector3(-171.34f, 0, -612.4f),
                new Vector3(-357.95f, 0, -120.94f),
                new Vector3(306.98f, 0, 305.65f),
                new Vector3(-384.15f, 0, 281.54f)
            };
            GetClosestOccult(SouthHorn);
        }
        else
        {
            GetClosestAetheryte();
        }
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

    public void GetClosestOccult(List<Vector3> Shards)
    {
        int shard = 1;
        string closest = "";
        float closestDistance = SquaredDistance(playerLocation.X, playerLocation.Z, fateLocation.X, fateLocation.Z);
        // manual corrections
        // Brain Drain south horn
        if (fateId == 1967)
        {
            closestAetheryte = "3";
        }
        // Eye to Eye north horn
        else if (fateId == 2075)
        {
            closestAetheryte = "2";
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

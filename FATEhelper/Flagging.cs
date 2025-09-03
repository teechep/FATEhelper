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
    private Vector3 fateLocation;
    private Vector3 playerLocation;
    public Flagging(Plugin plugin,Vector3 FateLocation, Vector3 PlayerLocation)
    {
        config = plugin.Configuration;
        agentMap = AgentMap.Instance();
        teleport = Telepo.Instance();
        teleport->UpdateAetheryteList();
        territoryId = agentMap->CurrentTerritoryId;
        closestAetheryte = string.Empty;
        closestAetheryteId = 0;
        fateLocation = FateLocation;
        playerLocation = PlayerLocation;
        GetClosestAetheryte();
    }
    
    private static int SquaredDistance(int x1, int y1, int x2, int y2)
    {
        x1 -= x2;
        y1 -= y2;
        return x1 * x1 + y1 * y1;
    }

    private void GetClosestAetheryte()
    {
        // aetheryte location data doesn't have a vertical coordinate
        // may cause a false positive in places like Yak T'el, but probably very few fringe cases
        int playerX = (int)playerLocation.X;
        int playerY = (int)playerLocation.Z;
        int shortestDistance = SquaredDistance(playerX, playerY, (int)fateLocation.X, (int)fateLocation.Z);
        var sheet = Plugin.DataManager.GetExcelSheet<Aetheryte>(Plugin.ClientState.ClientLanguage);
        var telelist = teleport->TeleportList;
        foreach (var row in sheet)
        {
            if (row.Territory.RowId == Plugin.ClientState.TerritoryType && row.IsAetheryte)
            {
                var marker = Plugin.DataManager.GetSubrowExcelSheet<MapMarker>().SelectMany(m => m).Cast<MapMarker?>()
                                   .FirstOrDefault(m => m!.Value.DataType == 3 && m.Value.DataKey.RowId == row.RowId);
                if (marker != null)
                {
                    // marker values add 1024 to be always positive, so subtract to align them with other location vectors
                    var distance = SquaredDistance((marker.Value.X - 1024), (marker.Value.Y - 1024),
                                                   (int)fateLocation.X, (int)fateLocation.Z);
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
}

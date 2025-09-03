using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game;
using CSFateManager = FFXIVClientStructs.FFXIV.Client.Game.Fate.FateManager;

namespace FATEhelper;

internal unsafe class Target
{
    internal Target(IntPtr address)
    {
        Address = address;
    }

    public IntPtr Address { get; }
    public FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject* Struct =>
        (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)Address;
}

internal unsafe class AutoSync
{
    private Plugin Plugin;
    private CSFateManager* fateManager;
    private Dictionary<string,uint[]> Tanks;

    public AutoSync(Plugin plugin)
    {
        Plugin = plugin;
        fateManager = CSFateManager.Instance();
        // icon ID of tank stance in status list, and action ID for tank stance action
        Tanks = new Dictionary<string, uint[]>
        {
            { "DRK", [213108, 3629] },
            { "GLA", [212506, 28] },
            { "GNB", [213603, 16142] },
            { "MRD", [212551, 48] },
            { "PAL", [212506, 28] },
            { "WAR", [212551, 48] }
        };
    }

    // check if player is tank
    private string IsTank()
    {
        if (Plugin.ClientState.LocalPlayer != null)
        {
            var job = Plugin.ClientState.LocalPlayer.ClassJob.Value.Abbreviation.ToString();
            if (Tanks.ContainsKey(job))
                return job;
        }
        return "";
    }

    public void LevelSync()
    {
        if (Plugin.TargetManager.Target != null)
        {
            var target = new Target(Plugin.TargetManager.Target.Address);
            var fateId = target.Struct->FateId;
            if (fateId != 0)
            {
                var currentId = fateManager->GetCurrentFateId();
                if (currentId == fateId)
                {
                    try
                    {
                        var curFate = fateManager->GetFateById(fateId);
                        // do not auto sync to boss fates
                        if (!fateManager->IsSyncedToFate(curFate) && curFate->Duration < 901)
                            fateManager->LevelSync();
                        string job = IsTank();
                        if (job != "" && Plugin.Configuration.AutoSyncTankStance)
                            EnableTankStance(job);
                        if (Plugin.Configuration.TargetForlorn)
                            TargetForlorn();
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.Error(e.Message);
                    }
                }
            }
        }
    }

    private void EnableTankStance(string job)
    {
        if (Plugin.ClientState.LocalPlayer != null)
        {
            // check player statuses to see if tank stance is already enabled
            foreach (var stat in Plugin.ClientState.LocalPlayer.StatusList)
            {
                if (stat.GameData.Value.Icon == Tanks[job][0])
                {
                    return;
                }
            }
            var action = ActionManager.Instance();
            // check that we're not currently casting
            if (action->GetActionStatus(ActionType.Action, Tanks[job][1]) == 0)
                action->UseAction(ActionType.Action, Tanks[job][1]);
        }
    }

    // putting this here to not recalculate current fate and target fate twice
    // 7586 is forlorn maiden, 7587 is the forlorn
    private void TargetForlorn()
    {
        var target = Plugin.TargetManager.Target;
        if (target != null && (target.DataId == 7586 || target.DataId == 7587))
            return;
        var objs = Plugin.ObjectTable;
        foreach (var obj in objs) 
        {
            if (obj.DataId == 7586 || obj.DataId == 7587)
            {
                if (obj.IsTargetable && !obj.IsDead)
                    Plugin.TargetManager.Target = obj;
                return;
            }
        }
    }
}

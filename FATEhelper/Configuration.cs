using Dalamud.Configuration;
using System;

namespace FATEhelper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public int SortBy { get; set; } = 0;
    public string[] SortOptions { get; set; } = {"Bonus first, then Time Remaining", "Time Remaining", "Bonus first, then Percent Completed", "Percent Completed"};
    public bool ShowObjectiveIcon { get; set; } = false;
    public bool ShowFateNames { get; set; } = true;
    public bool ShowCurrency { get; set; } = false;
    public int FontSize { get; set; } = 1;
    public string[] FontOptions { get; set; } = { "Small", "Regular", "Large" };
    public bool LimitDisplay { get; set; } = false;
    public int Limit { get; set; } = 1;
    public string[] LimitOptions { get; set; } = { "2", "3", "4", "5", "6" };
    public bool OpenMapWithFlag { get; set; } = false;
    public bool TeleportWithFlag { get; set; } = true;
    public bool ShowAetheryteName { get; set; } = false;
    public bool ShowCompass { get; set; } = false;

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

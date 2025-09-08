using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FATEhelper.Windows;

namespace FATEhelper;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; set; } = null!;
    [PluginService] internal static IFateTable FateTable { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private const string CommandMain = "/fatehelper";
    private const string CommandConfig = "/fatehelperconfig";
    
    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("FATEhelper");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private FateHelper FateHelper { get; init; }
    
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        FateHelper = new FateHelper(this);
        
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        
        CommandManager.AddHandler(CommandMain, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle FATE list."
        });
        CommandManager.AddHandler(CommandConfig, new CommandInfo(OnCommand)
        {
            HelpMessage = "Show FATE Helper configuration window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandMain);
        CommandManager.RemoveHandler(CommandConfig);
        
    }
    
    // get and return fate info to update main window
    internal List<FateInfo> ReturnFateInfo()
    {
        return FateHelper.GetFateInfo();
    }
    
    // flag fate when clicking the name, and open map/teleport if desired
    internal void FateFlag(Vector3 location)
    {
        if(ClientState.LocalPlayer == null)
            return;
        var flag = new Flagging(this, location, ClientState.LocalPlayer.Position);
        flag.FlagFate();
    }
    
    // show closest aetheryte next to fate name for controller users
    internal string ClosestAetheryte(Vector3 location)
    {
        if(ClientState.LocalPlayer == null)
            return string.Empty;
        var flag = new Flagging(this, location, ClientState.LocalPlayer.Position);
        return flag.GetClosestName();
    }

    private void OnCommand(string command, string args)
    {
        if(command == CommandMain)
            ToggleMainUI();
        else if(command == CommandConfig)
            ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}

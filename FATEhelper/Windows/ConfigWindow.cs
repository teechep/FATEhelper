using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace FATEhelper.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("FATE Helper Config###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(130, 20),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("tabs"))
        {
            if (ImGui.BeginTabItem("Configuration"))
            {
                ImGui.Dummy(new Vector2(0,5));
                var tankStance = Configuration.AutoSyncTankStance;
                if (ImGui.Checkbox("Enable Tank Stance along with automatic level sync", ref tankStance))
                {
                    Configuration.AutoSyncTankStance = tankStance;
                    Configuration.Save();
                }
                var forlorn = Configuration.TargetForlorn;
                if (ImGui.Checkbox("Automatically target Forlorns when they spawn", ref forlorn))
                {
                    Configuration.TargetForlorn = forlorn;
                    Configuration.Save();
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("You will be unable to target a different FATE enemy as long as a Forlorn is up."); 
                ImGui.Dummy(new Vector2(0,10));
                int font = Configuration.FontSize;
                ImGui.TextUnformatted("FATE Window Text Size");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("This scales the text, so Large will appear blurry."); 
                ImGui.PushID("fontoptions"); 
                if (ImGui.Combo("", ref font, Configuration.FontOptions, Configuration.FontOptions.Length))
                { 
                    Configuration.FontSize = font;
                    Configuration.Save();
                }
                ImGui.PopID();
                ImGui.Dummy(new Vector2(0, 15));
                var limitDisplay = Configuration.LimitDisplay;
                if (ImGui.Checkbox(" Limit the number of FATEs shown to:", ref limitDisplay))
                {
                    Configuration.LimitDisplay = limitDisplay;
                    Configuration.Save();
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Un-check this for no limit.");
                int limit = Configuration.Limit;
                ImGui.PushID("limit");
                if (ImGui.Combo("", ref limit, Configuration.LimitOptions, Configuration.LimitOptions.Length))
                {
                    Configuration.Limit = limit;
                    Configuration.Save();
                }
                 ImGui.PopID();
                 ImGui.Dummy(new Vector2(0, 15));
                 int sort = Configuration.SortBy;
                 ImGui.TextUnformatted("Sort FATEs by:");
                 ImGui.PushID("sortby");
                 if (ImGui.Combo("", ref sort, Configuration.SortOptions, Configuration.SortOptions.Length))
                 {
    
                     Configuration.SortBy = sort;
     
                     Configuration.Save();
                     
                 }
                 ImGui.PopID();
                 ImGui.Dummy(new Vector2(0, 15));
                 var obj = Configuration.ShowObjectiveIcon;
                 if (ImGui.Checkbox("Show objective icons for NPC-started FATEs", ref obj))
                 {
                     Configuration.ShowObjectiveIcon = obj;
                     Configuration.Save();
                 }
                 if(ImGui.IsItemHovered())
                     ImGui.SetTooltip("When checked, NPC-started FATE icons will show the type of FATE rather than the exclamation point icon.");
                 var names = Configuration.ShowFateNames;
                 if (ImGui.Checkbox("Show FATE names (disable if you want a thin window)", ref names))
                 {
                     Configuration.ShowFateNames = names;
                     Configuration.Save();
                 }
                 var curr = Configuration.ShowCurrency;
                 if (ImGui.Checkbox("Show Grand Company seal / Bicolor Gemstone count", ref curr))
                 {
                     Configuration.ShowCurrency = curr; 
                     Configuration.Save();
                 }
                 var openmap = Configuration.OpenMapWithFlag;
                 if (ImGui.Checkbox("When clicking on a FATE name/flag, open the map", ref openmap))
                 {
                     Configuration.OpenMapWithFlag = openmap;
                     Configuration.Save();
                 }
                 var teleport = Configuration.TeleportWithFlag;
                 if (ImGui.Checkbox("When clicking on a FATE name/flag, teleport to closest aetheryte", ref teleport))
                 {
                     Configuration.TeleportWithFlag = teleport;
                     Configuration.Save();
                 }
                 if(ImGui.IsItemHovered())
                     ImGui.SetTooltip("You will only teleport if it is faster than flying to the FATE from your current position.");
                 var aetheryte = Configuration.ShowAetheryteName;
                 if (ImGui.Checkbox("Show closest aetheryte next to FATE name (for controller users)", ref aetheryte))
                 {
                     Configuration.ShowAetheryteName = aetheryte;
                     Configuration.Save();
                 }
                 if(ImGui.IsItemHovered())
                     ImGui.SetTooltip("This will also only show the aetheryte if teleporting is faster than flying.");
                 ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("FAQ"))
            {
                ImGui.TextUnformatted("Why are some of the timers counting up?");
                ImGui.TextUnformatted(
                    "These are for FATEs that are started by talking to an NPC, and FATEs in a boss spawning chain. Most, if not all, of the NPC FATEs will\n" +
                    "disappear after 10 minutes of not talking to the NPC. The boss chain FATEs will spawn 2-3 minutes after appearing in the list.\n" +
                    "PLEASE NOTE: when you first spawn into a zone or first load the plugin window, these count-up timers are NOT accurate. Any\n" +
                    "NPC FATEs listed when you spawn in will disappear before the usual 10 minutes.");
                ImGui.Dummy(new Vector2(0, 20));
                ImGui.TextUnformatted("Why are some FATEs listed but they're not on the map?");
                ImGui.TextUnformatted(
                    "These are FATEs in a \"pre-spawn\" state and will usually appear within one minute. The flags and aetherytes listed for these FATEs\n" +
                    "are still accurate, so you can head there early. Just don't mention in-game that you knew the FATE was going to spawn, Square\n" + 
                    "doesn't like us talking about mods. Also be courteous to other players, and give them time to arrive before completing the FATE.");
                ImGui.Dummy(new Vector2(0,20));
                ImGui.TextUnformatted("Why are the NPC FATE flags slightly off / Why isn't the name the same as the map?");
                ImGui.TextUnformatted("The position (and name) of the NPC FATEs is for the FATE itself, not the NPC that you need to talk to. But in all my\n" +
                                      "many, many hours of FATE grinding, I've never seen an NPC that wasn't inside of the FATE radius. There's no direct\n" +
                                      "correlation between NPC and FATE, so I decided it wasn't worth the extra programming expense to get the NPC information.");
                ImGui.Dummy(new Vector2(0,20));
                ImGui.TextUnformatted("Why does the automatic teleport not work sometimes?");
                ImGui.TextUnformatted("You will not teleport if you could fly to the FATE faster. If you haven't unlocked flying in that area yet, teleporting could be\n" +
                                      "faster. But in that case, please, for your own sanity, spend your time unlocking flying first.");
                ImGui.Dummy(new Vector2(0,20));
                ImGui.TextUnformatted("I turned on the aetheryte name setting, why do some FATEs not have it?");
                ImGui.TextUnformatted("Same as above, it won't show if flying is faster. This is also why the name will go away when you get close enough to the FATE.");
                ImGui.Dummy(new Vector2(0,20));
                ImGui.TextUnformatted("Why can't I turn off level sync?");
                ImGui.TextUnformatted("The level sync detection is based on your current target. Unfocus what you're targeted on, and then you can turn off level sync.\n"+
                                      "As usual, you can also run outside of the FATE radius to turn it off.");
                ImGui.Dummy(new Vector2(0,20));
                ImGui.TextUnformatted("What about rare boss / field operation FATEs?");
                ImGui.TextUnformatted("Automatic level sync will not work when the initial FATE duration is over 15 minutes, even when the time remaining on the FATE\n" +
                                      "goes under 15 minutes. This is to prevent accidentally pulling the boss early.");
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
}

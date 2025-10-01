using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Textures;

namespace FATEhelper.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private Configuration Configuration;
    private List<FateInfo> Info;
    private float fontSize;
    private float timerWidth;
    private float progressWidth;
    
    
    public MainWindow(Plugin plugin)
        : base("FATE Helper##FATEhelper90210", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(130, 20),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Plugin = plugin;
        Configuration = Plugin.Configuration;
        Info =  new List<FateInfo>();
    }

    public void Dispose() { }

    //get the max width of text for the timer and percent columns, for right alignment
    private void GetWidths(List<FateInfo> fates)
    {
        timerWidth = 0;
        progressWidth = 0;
        foreach (var fate in fates)
        {
            var timer = GetTimer(fate.TimeRemaining, fate.NotStarted);
            var width = ImGui.CalcTextSize(timer).X;
            if (width > timerWidth)
            {
                timerWidth = width;
            }
            width = ImGui.CalcTextSize(fate.Progress+"%").X;
            if (width > progressWidth)
            {
                progressWidth = width;
            }
        }
    }

    // return the formatted time remaining
    private ImU8String GetTimer(long time, bool started)
    {
        TimeSpan tr = TimeSpan.FromSeconds(time);
        // I don't think any fates go over half an hour but just in case
        return (started ? "(+" : "") + (time > 3599 ? tr.ToString(@"h\:mm\:ss") : tr.ToString(@"m\:ss")) + (started ? ")" : "");
    }

    // draw the icons slightly higher so they align with the text
    private void DrawIcon(uint iconId,float scale = 1.4f)
    {
        var pos = ImGui.GetCursorPos();
        pos.Y -= fontSize*0.2f;
        ImGui.SetCursorPos(pos);
        var icon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrDefault();
        if (icon != null)
        {
            ImGui.Image(icon.Handle, new Vector2(fontSize * scale, fontSize * scale));
        }
    }
    
    // align timer and percent to the right
    private void AlignRight(ImU8String content,string contentType)
    {
        var posX = ImGui.GetCursorPosX();
        float offset = 0;
        if (contentType == "timer")
        {
            offset = timerWidth - ImGui.CalcTextSize(content).X;
        }
        else if (contentType == "progress")
        {
            offset = progressWidth - ImGui.CalcTextSize(content).X;
        }
        ImGui.SetCursorPosX(posX+offset);
        ImGui.TextUnformatted(content);
    }

    public void FateWindow()
    {
        fontSize = ImGui.GetFontSize();
        // get new fate info
        Info = Plugin.ReturnFateInfo(); 
        if (Configuration.FontSize == 0)
            ImGui.SetWindowFontScale(0.7f);
        else if (Configuration.FontSize == 2)
            ImGui.SetWindowFontScale(1.4f);
        else
            ImGui.SetWindowFontScale(1);
        GetWidths(Info);
        if (Info.Count > 0)
        {
            // larger gap for non-name flag buttons to move up so that text is centered
            if(!Configuration.ShowFateNames)
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding,new Vector2(4,10));
            else
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 5));
            if (ImGui.BeginTable("fatetable", 4))
            {
                ImGui.TableSetupColumn("icon");
                if (Configuration.ShowFateNames)
                    ImGui.TableSetupColumn("name");
                ImGui.TableSetupColumn("time");
                ImGui.TableSetupColumn("progress");
                if (!Configuration.ShowFateNames)
                    ImGui.TableSetupColumn("flag");
                int limit = Configuration.LimitDisplay ? Configuration.Limit + 2 : Info.Count;
                if (limit > Info.Count)
                    limit = Info.Count;
                for (var it = 0; it < limit; it++)
                {
                    var i = Info[it];
                    ImGui.TableNextColumn();
                    DrawIcon(i.IconId);
                    if (i.IsBonus)
                    {
                        var pos = ImGui.GetCursorPos();
                        pos.X += (fontSize * 0.4f);
                        pos.Y -= (fontSize * 1.7f);
                        ImGui.SetCursorPos(pos);
                        ImGui.SetItemAllowOverlap();
                        DrawIcon(60934, 1.6f);
                    }

                    ImGui.TableNextColumn();
                    // fate name with closest aetheryte name
                    if (Configuration.ShowFateNames)
                    {
                        string suffix = "";
                        if (Configuration.ShowAetheryteName)
                        {
                            var closestAetheryte = Plugin.ClosestAetheryte(i.Position);
                            if (!string.IsNullOrEmpty(closestAetheryte))
                                suffix += " (" + closestAetheryte + ")";
                        }

                        if (ImGui.Selectable($"{i.Name}{suffix}"))
                        {
                            Plugin.FateFlag(i.Position);
                        }

                        ImGui.TableNextColumn();
                    }

                    // time remaining
                    AlignRight(GetTimer(i.TimeRemaining, i.NotStarted), "timer");
                    ImGui.TableNextColumn();
                    // progress
                    AlignRight(i.Progress + "%", "progress");
                    if (i.CollectCount > 0)
                    {
                        ImGui.SameLine();
                        if (i.CollectCount != 9999)
                        {
                            ImGui.TextUnformatted($"({i.CollectCount})");
                            ImGui.SameLine();
                        }

                        // I'm fairly sure all the turn-in fates are 6 items for full credit but lmk if that's wrong
                        DrawIcon((uint)(i.CollectCount < 6 ? 61502 : 60081));
                    }

                    // show flag button if not showing fate name
                    if (!Configuration.ShowFateNames)
                    {
                        var flag = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(60561));
                        if (flag.TryGetWrap(out var flagWrap, out _))
                        {
                            ImGui.TableNextColumn();
                            var cursor = ImGui.GetCursorPosY();
                            cursor -= fontSize * 0.4f;
                            ImGui.SetCursorPosY(cursor);
                            // interactive elements need unique id, button text label would normally provide it
                            ImGui.PushID($"button{it}");
                            if (ImGui.ImageButton(flagWrap.Handle, new Vector2(fontSize * 1.4f, fontSize * 1.4f)))
                            {
                                Plugin.FateFlag(i.Position);
                            }

                            ImGui.PopID();
                        }
                    }
                }
                ImGui.EndTable();
            }
            ImGui.PopStyleVar();
        }
        else
        {
            ImGui.TextUnformatted("No active FATEs.");
            if (Configuration.ShowCurrency)
                ImGui.Dummy(new Vector2(0,10));
        }
        if (Configuration.ShowCurrency && Plugin.ClientState.IsLoggedIn)
        {
            // pre vs post shadowbringers areas, I'm pretty sure lakeland is first with an 813 id
            // 65004 is maelstrom, 65005 adder, 65006 flames, same order of 1/2/3 for the grand company
            int iconId = Plugin.ClientState.TerritoryType < 813 ? (FateHelper.GrandCompany + 65003) : 65071;
            DrawIcon((uint)iconId);
            ImGui.SameLine();
            // move text down a little to center with icon
            var pos = ImGui.GetCursorPosY();
            pos += (fontSize * 0.2f);
            ImGui.SetCursorPosY(pos);
            ImGui.TextUnformatted($"{FateHelper.FateCurrency}");
        }
    }

    public override void Draw() => FateWindow();
}

using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;

namespace VeryImportantItem;

public class MainWindow : Window, IDisposable {
    private readonly Configuration configuration;
    private readonly Vector4 colorRed = new(1, 0.25f, 0.25f, 1);

    private string itemNameOrId = string.Empty;
    private string itemError = string.Empty;

    public MainWindow(Plugin plugin) : base(
        "Very Important Item###VeryImportantItem",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
    ) {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(610, 440),
            MaximumSize = new Vector2(800, 1000)
        };

        configuration = plugin.Configuration;
    }

    public void Dispose() {}

    public override void Draw() {
        // settings
        var highlightName = configuration.HighlightItemNamesInTooltips;
        if (ImGui.Checkbox("Highlight item names in tooltips", ref highlightName)) {
            configuration.HighlightItemNamesInTooltips = highlightName;
            configuration.Save();
        }

        var playSoundEffect = configuration.PlaySoundEffect;
        if (ImGui.Checkbox("Play sound effect when showing a tooltip of an important item", ref playSoundEffect)) {
            configuration.PlaySoundEffect = playSoundEffect;
            configuration.Save();
        }

        var addContextMenuEntries = configuration.AddContextMenuEntries;
        if (ImGui.Checkbox("Add entries to item context menus", ref addContextMenuEntries)) {
            configuration.AddContextMenuEntries = addContextMenuEntries;
            configuration.Save();
        }
        ImGuiComponents.HelpMarker("Adds entries to item context menus that allow adding and removing very important items.");

        // item form
        ImGui.Separator();
        ImGui.Text("Item name or ID:");
        bool pressedEnter;
        using (ImRaii.ItemWidth(200)) {
            pressedEnter = ImGui.InputText("##itemNameOrID", ref itemNameOrId, flags: ImGuiInputTextFlags.EnterReturnsTrue);
        }

        ImGui.SameLine();
        if (ImGui.Button("Add item") || pressedEnter) {
            var inputIsId = uint.TryParse(itemNameOrId, out var inputItemId);
            var items = Plugin.DataManager.Excel.GetSheet<Item>().Where(item => {
                if (inputIsId) {
                    return item.RowId == inputItemId;
                }

                return string.Equals(item.Name.ExtractText(), itemNameOrId, StringComparison.CurrentCultureIgnoreCase);
            }).ToArray();

            switch (items.Length) {
                case 0:
                    itemError = "No matching items found.";
                    break;

                case > 1:
                    itemError = "Multiple matching items found, please be more specific.";
                    break;

                case 1:
                    var itemId = items[0].RowId;
                    if (!configuration.ImportantItems.Contains(itemId)) {
                        configuration.ImportantItems.Add(itemId);
                        configuration.Save();
                        itemError = string.Empty;
                        itemNameOrId = string.Empty;
                    } else {
                        itemError = "This item has already been added.";
                    }
                    break;
            }
        }

        // error
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, colorRed)) {
            ImGui.Text(itemError);
        }

        // table of saved items
        ImGui.Spacing();
        using (ImRaii.Child("importantItemsChild")) {
            using (ImRaii.Table("importantItemsTable", 3, ImGuiTableFlags.BordersInner)) {
                ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("###removeItemColumn", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableHeadersRow();

                uint? removeItemId = null;
                foreach (var itemId in configuration.ImportantItems) {
                    var row = Plugin.DataManager.Excel.GetSheet<Item>().GetRowOrDefault(itemId);
                    if (row is null) {
                        continue;
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(itemId.ToString());
                    ImGui.TableNextColumn();
                    ImGui.Text(row?.Name.ExtractText());
                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.Button($"Remove###remove{itemId}")) {
                        removeItemId = itemId;
                    }
                }

                if (removeItemId is not null) {
                    configuration.ImportantItems.Remove((uint) removeItemId);
                    configuration.Save();
                }
            }
        }
    }
}

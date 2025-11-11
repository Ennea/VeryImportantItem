using System;
using System.Diagnostics;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace VeryImportantItem;

/**
 * TODO:
 * - in the chat messages when adding/removing via context menu, use item payloads
 * - color item names in chat, too (no animation)
 * - maybe stop destructive actions: selling, desynthing, GC turn-ins
 */
public sealed class Plugin : IDalamudPlugin {
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("VeryImportantItem");
    public Configuration Configuration { get; init; }
    private MainWindow MainWindow { get; init; }
    private Util Util { get; init; }

    private const string CommandName = "/pvii";
    private readonly Stopwatch rainbowAnimationWatch = new();

    public Plugin() {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Util = new Util(this);

        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "Opens the configuration for Very Important Item."
        });

        AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "ItemDetail", ItemDetailPreDraw);
        AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "ItemDetail", ItemDetailPostDraw);

        ContextMenu.OnMenuOpened += ContextMenuOpened;

        rainbowAnimationWatch.Start();
    }

    public void Dispose() {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        AddonLifecycle.UnregisterListener(ItemDetailPreDraw);
        AddonLifecycle.UnregisterListener(ItemDetailPostDraw);

        ContextMenu.OnMenuOpened -= ContextMenuOpened;

        rainbowAnimationWatch.Stop();
    }

    private void ToggleMainUi() => MainWindow.Toggle();
    private void OnCommand(string command, string args) => ToggleMainUi();

    private unsafe void ItemDetailPreDraw(AddonEvent type, AddonArgs args) {
        // play a sound effect
        var itemId = ItemUtil.GetBaseId(AgentItemDetail.Instance()->ItemId).ItemId;
        if (!Configuration.ImportantItems.Contains(itemId)) {
            Util.SetLastItem(itemId);
            return;
        }

        Util.PlaySound(itemId);

        // advance the rainbow text animation
        if (rainbowAnimationWatch.Elapsed >= TimeSpan.FromMilliseconds(50)) {
            Util.AdvanceRainbowOffset();
            rainbowAnimationWatch.Restart();
        }

        // modify the look of the item name
        if (!Configuration.HighlightItemNamesInTooltips) {
            return;
        }

        var atkBase = args.Addon;
        if (atkBase.IsNull || !atkBase.IsVisible) {
            return;
        }

        var tooltipTitleAtk = ((AtkUnitBase*) atkBase.Address)->GetTextNodeById(33);
        if (tooltipTitleAtk is null || !tooltipTitleAtk->IsVisible()) {
            return;
        }

        tooltipTitleAtk->SetText(Util.BuildRainbowSeString(tooltipTitleAtk->GetText().ExtractText()));
    }

    private unsafe void ItemDetailPostDraw(AddonEvent type, AddonArgs args) {
        // this is called once with an item id of 0 when the tooltip disappears, so let's use it
        if (AgentItemDetail.Instance()->ItemId == 0) {
            Util.ClearLastItem();
        }
    }

    private void ContextMenuOpened(IMenuOpenedArgs args) {
        // grabbing `HoveredItem` isn't consistent (e.g. it's randomly (?) 0
        // when right-clicking ingredients in the crafting log), but for now
        // it's good enough
        var (itemId, itemKind) = ItemUtil.GetBaseId((uint) GameGui.HoveredItem);
        if (itemId == 0 || (itemKind != ItemKind.Normal && itemKind != ItemKind.Hq)) {
            return;
        }

        var menuItem = new MenuItem {
            Name = !Configuration.ImportantItems.Contains(itemId)
                       ? "Add very important item"
                       : "Remove very important item",
            PrefixChar = 'V',
            PrefixColor = 522,
            OnClicked = _ => {
                var itemName = DataManager.Excel.GetSheet<Item>().GetRowOrDefault(itemId)?.Name.ExtractText() ??
                               "<invalid item>";
                if (!Configuration.ImportantItems.Contains(itemId)) {
                    Configuration.ImportantItems.Add(itemId);
                    ChatGui.Print($"{itemName} added to the list of very important items.");
                } else {
                    Configuration.ImportantItems.Remove(itemId);
                    ChatGui.Print($"{itemName} removed from the list of very important items.");
                }

                Configuration.Save();
            }
        };

        args.AddMenuItem(menuItem);
    }
}

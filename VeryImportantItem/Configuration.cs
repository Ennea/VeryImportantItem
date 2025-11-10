using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace VeryImportantItem;

[Serializable]
public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public bool HighlightItemNamesInTooltips { get; set; } = true;
    public bool PlaySoundEffect { get; set; } = true;
    public List<uint> ImportantItems { get; set; } = [];

    public void Save() {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Text;

namespace VeryImportantItem;

public class Util {
    private readonly Configuration configuration;

    // pre-computed, 36 values. converted from LCH to sRGB,
    // each value being (70, 50, i * 10) in LCH space
    private readonly byte[,] rainbowColors = {
        { 252, 131, 173 },
        { 254, 132, 158 },
        { 253, 134, 142 },
        { 250, 137, 128 },
        { 245, 141, 115 },
        { 237, 147, 103 },
        { 228, 152,  92 },
        { 217, 158,  84 },
        { 205, 164,  78 },
        { 191, 169,  76 },
        { 176, 174,  76 },
        { 161, 178,  80 },
        { 144, 182,  87 },
        { 126, 186,  97 },
        { 107, 189, 109 },
        {  86, 191, 122 },
        {  61, 192, 137 },
        {  18, 193, 153 },
        {   0, 193, 169 },
        {   0, 192, 183 },
        {   0, 191, 195 },
        {   0, 190, 207 },
        {   0, 188, 219 },
        {   0, 187, 232 },
        {   0, 185, 246 },
        {  55, 182, 254 },
        {  97, 178, 254 },
        { 123, 173, 254 },
        { 145, 168, 254 },
        { 164, 163, 254 },
        { 184, 157, 250 },
        { 202, 151, 241 },
        { 217, 145, 230 },
        { 230, 140, 217 },
        { 240, 136, 203 },
        { 247, 133, 188 }
    };

    private long lastSound;
    private uint lastItemId;
    private long rainbowOffset;

    public Util(Plugin plugin) {
        configuration = plugin.Configuration;
    }

    public void ClearLastItem() {
        lastItemId = 0;
    }

    public void SetLastItem(uint itemId) {
        lastItemId = itemId;
    }

    public void PlaySound(uint itemId) {
        if (!configuration.PlaySoundEffect) {
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - lastSound < 250) {
            return;
        }

        if (itemId == lastItemId) {
            return;
        }

        lastSound = now;
        lastItemId = itemId;
        UIGlobals.PlayChatSoundEffect(15);
    }

    public void AdvanceRainbowOffset() {
        rainbowOffset = (rainbowOffset + 1) % rainbowColors.GetLength(0);
    }

    public ReadOnlySpan<Byte> BuildRainbowSeString(string str) {
        var builder = new SeStringBuilder().PushColorRgba(255, 255, 255, 255);

        uint index = 0;
        foreach (var c in str) {
            var colorIndex = (rainbowOffset + (index++ / 2)) % rainbowColors.GetLength(0);
            builder = builder
                .PushEdgeColorRgba(
                    rainbowColors[colorIndex, 0],
                    rainbowColors[colorIndex, 1],
                    rainbowColors[colorIndex, 2],
                    255
                )
                .Append(c)
                .PopEdgeColor();
        }

        return builder.PopColor().GetViewAsSpan();
    }
}

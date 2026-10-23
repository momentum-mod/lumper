namespace Lumper.UI.ViewModels.Shared.Vtf;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Converts HDR (RGBA16161616F) image data to RGBA8888 for display.
///
/// vtfpp just clamps and scales the linear values, which comes out much darker than it should. Instead we clamp to
/// 0-1, gamma-encode RGB with 1/2.2 through a 10-bit lookup table, then drop to 8 bits. Alpha is left linear.
/// </summary>
internal static class HdrImageConversion
{
    private const float Gamma = 2.2f;

    // 10-bit linear value -> 8-bit gamma-encoded value (gamma-encoded at 10 bits, then shifted down by 2)
    private static readonly byte[] LinearToGamma = BuildGammaTable();

    public static byte[] Rgba16161616FToRgba8888(ReadOnlySpan<byte> data, int pixelCount)
    {
        ReadOnlySpan<Half> halves = MemoryMarshal.Cast<byte, Half>(data);
        if (halves.Length < pixelCount * 4)
            return [];

        byte[] rgba = new byte[pixelCount * 4];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            rgba[i] = LinearToGamma[To10Bit((float)halves[i])];
            rgba[i + 1] = LinearToGamma[To10Bit((float)halves[i + 1])];
            rgba[i + 2] = LinearToGamma[To10Bit((float)halves[i + 2])];
            rgba[i + 3] = (byte)(To10Bit((float)halves[i + 3]) >> 2);
        }

        return rgba;
    }

    private static int To10Bit(float value) => Math.Clamp((int)((1023.0f * value) + 0.5f), 0, 1023);

    private static byte[] BuildGammaTable()
    {
        byte[] table = new byte[1024];
        for (int i = 0; i < table.Length; i++)
            table[i] = (byte)((ushort)((1023.0f * MathF.Pow(i / 1023.0f, 1.0f / Gamma)) + 0.5f) >> 2);

        return table;
    }
}

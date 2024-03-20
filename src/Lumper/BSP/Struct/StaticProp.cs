namespace Lumper.Lib.BSP.Struct;
using System.Drawing;

public class StaticProp
{
    // v4
    public Vector Origin { get; set; }          // origin
    public Angle Angle { get; set; }            // orientation (pitch yaw roll)

    // v4
    public ushort PropType { get; set; }        // index into model name dictionary
    public ushort FirstLeaf { get; set; }       // index into leaf array
    public ushort LeafCount { get; set; }
    public byte Solid { get; set; }             // solidity type
                                                // every version except v7*
    public byte Flags { get; set; }
    // v4 still
    public int Skin { get; set; }               // model skin numbers
    public float FadeMinDist { get; set; }
    public float FadeMaxDist { get; set; }
    public Vector LightingOrigin { get; set; }  // for lighting
    // since v5
    public float ForcedFadeScale { get; set; }  // fade distance scale
    // v6, v7, and v7* only
    public ushort MinDXLevel { get; set; }      // minimum DirectX version to be visible
    public ushort MaxDXLevel { get; set; }      // maximum DirectX version to be visible
    // v7* only
    public uint FlagsV7s { get; set; }
    public ushort LightmapResX { get; set; }    // lightmap image width
    public ushort LightmapResY { get; set; }    // lightmap image height
    // since v8
    public byte MinCPULevel { get; set; }
    public byte MaxCPULevel { get; set; }
    public byte MinGPULevel { get; set; }
    public byte MaxGPULevel { get; set; }
    // since v7
    public Color DiffuseModulation { get; set; } // per instance color and alpha modulation
    // v9 and v10 only
    public bool DisableX360 { get; set; }       // if true, don't show on XBox 360 (4-bytes long)
    // since v10
    public uint FlagsEx { get; set; }           // Further bitflags.
    // since v11
    public float UniformScale { get; set; }      // Prop scale
}

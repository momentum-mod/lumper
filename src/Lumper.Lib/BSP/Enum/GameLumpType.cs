namespace Lumper.Lib.BSP.Enum;

// Gamelump ID is a 4 byte int with the value being the ASCII code
// So the names are just the hex value here,
// e.g. "sprp" in ASCII = "0x73707270" in hexadecimal
public enum GameLumpType
{
    Unknown = 0,
    sprp = 0x73707270, // (1936749168) static prop
    dprp = 0x64707270, // detail prop
    dplt = 0x64706c74, // detail prop lighting LDR
    dplh = 0x64706c68, // detail prop lighting HDR
    pmti =
        0x706d7469 // (1886221417) from BSPConvert
    ,
}

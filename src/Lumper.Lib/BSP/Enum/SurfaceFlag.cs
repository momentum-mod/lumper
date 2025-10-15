namespace Lumper.Lib.Bsp.Enum;

[System.Flags]
public enum SurfaceFlag : int
{
    None = 0,
    Light = 1 << 0,
    Sky2d = 1 << 1,
    Sky = 1 << 2,
    Warp = 1 << 3,
    Trans = 1 << 4,
    Noportal = 1 << 5,
    Trigger = 1 << 6,
    Nodraw = 1 << 7,
    Hint = 1 << 8,
    Skip = 1 << 9,
    Nolight = 1 << 10,
    Bumplight = 1 << 11,
    Noshadows = 1 << 12,
    Nodecals = 1 << 13,
    Nochop = 1 << 14,
    Hitbox = 1 << 15,
    SkyNoEmit = 1 << 16,
    SkyOcclusion = 1 << 17,
    Slick = 1 << 18,
}

namespace Lumper.Lib.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;

public class ChangeTextureTask : LumperTask
{
    public override string Type { get; } = "ChangeTextureTask";
    public ChangeTextureTask()
    { }
    public Dictionary<string, string> Replace { get; set; } = [];
    public List<KeyValuePair<Regex, string>> ReplaceRegex { get; set; } = [];

    public override TaskResult Run(BspFile bsp)
    {
        TexDataLump texDataLump = bsp.GetLump<TexDataLump>();

        Progress.Max = texDataLump.Data.Count;
        foreach (TexData texture in texDataLump.Data)
        {
            Console.Write($"TexName: {texture.TexName}");
            if (Replace.TryGetValue(texture.TexName, out var value))
            {
                texture.TexName = value;
                Console.Write($" replace: {texture.TexName}");
            }
            else
            {
                foreach (KeyValuePair<Regex, string> replaceRegex in ReplaceRegex)
                {
                    var tmp = replaceRegex.Key.Replace(
                        texture.TexName,
                        replaceRegex.Value);

                    if (texture.TexName != tmp)
                    {
                        texture.TexName = tmp;
                        Console.Write($" replaceRegex: {texture.TexName}");
                    }
                }
            }
            Console.WriteLine();
            Progress.Count++;
        }
        return TaskResult.Success;
    }
}

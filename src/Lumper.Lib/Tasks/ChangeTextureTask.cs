namespace Lumper.Lib.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Struct;
using NLog;

public class ChangeTextureTask : LumperTask
{
    public override string Type => "ChangeTextureTask";

    public Dictionary<string, string> Replace { get; set; } = [];
    public List<KeyValuePair<Regex, string>> ReplaceRegex { get; set; } = [];

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public override TaskResult Run(BspFile bsp)
    {
        TexDataLump texDataLump = bsp.GetLump<TexDataLump>();

        Progress.Max = texDataLump.Data.Count;
        foreach (TexData texture in texDataLump.Data)
        {
            if (Replace.TryGetValue(texture.TexName, out var value))
            {
                _logger.Info($"Replaced {texture.TexName} with {value}");
                texture.TexName = value;
            }
            else
            {
                foreach (var newName in ReplaceRegex
                    .Select(replaceRegex => replaceRegex.Key.Replace(texture.TexName, replaceRegex.Value))
                    .Where(newName => texture.TexName != newName))
                {
                    _logger.Info($"Replaced {texture.TexName} with {newName}");
                }
            }
            Progress.Count++;
        }

        return TaskResult.Success;
    }
}

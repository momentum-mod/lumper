using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps.BspLumps;

namespace Lumper.Tasks
{
    public class ChangeTextureTask : LumperTask
    {
        public override string Type { get; } = "ChangeTextureTask";
        public ChangeTextureTask()
        { }
        public Dictionary<string, string> Replace { get; set; } = new();
        public List<KeyValuePair<Regex, string>> ReplaceRegex { get; set; } = new();
        public override TaskResult Run(BspFile map)
        {
            var texDataLump = map.GetLump<TexDataLump>();
            Progress.Max = texDataLump.Data.Count;
            foreach (var texture in texDataLump.Data)
            {
                Console.Write($"TexName: {texture.TexName}");
                if (Replace.ContainsKey(texture.TexName))
                {
                    texture.TexName = Replace[texture.TexName];
                    Console.Write($" replace: {texture.TexName}");
                }
                else
                {
                    foreach (var replaceRegex in ReplaceRegex)
                    {
                        string tmp = replaceRegex.Key.Replace(
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
}
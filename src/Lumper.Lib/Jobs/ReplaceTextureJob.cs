namespace Lumper.Lib.Jobs;

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BSP;
using BSP.Lumps.BspLumps;
using BSP.Struct;
using NLog;

public class ReplaceTextureJob : Job, IJob
{
    public static string JobName => "Replace Textures";
    public override string JobNameInternal => JobName;

    public List<Replacer> Replacers { get; set; } = [];

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override bool Run(BspFile bsp)
    {
        TexDataLump texDataLump = bsp.GetLump<TexDataLump>();

        Progress.Max = texDataLump.Data.Count;

        foreach (Replacer replacer in Replacers)
            replacer.Prepare();

        var counter = 0;
        foreach (TexData texture in texDataLump.Data)
        {
            var name = texture.TexName;
            if (Replacers.Any(replacer => replacer.TryReplace(texture)))
            {
                Logger.Info($"Replaced {name} with {texture.TexName}");
                counter++;
            }

            Progress.Count++;
        }

        Logger.Info($"Replaced {counter} textures");
        return true;
    }

    public class Replacer
    {
        public string Matcher { get; set; } = "";
        public string ReplaceWith { get; set; } = "";
        public bool IsRegex { get; set; }
        private Regex? _regexMatcher;

        public void Prepare()
        {
            if (!IsRegex || string.IsNullOrEmpty(Matcher))
                return;

            _regexMatcher = new Regex(Matcher);
        }


        public bool TryReplace(TexData texture)
        {
            if (IsRegex)
            {
                if (_regexMatcher is null)
                    return false;

                var newName = _regexMatcher.Replace(texture.TexName, ReplaceWith);
                if (texture.TexName == newName)
                    return false;

                texture.TexName = newName;
                return true;
            }
            else
            {
                if (texture.TexName != Matcher)
                    return false;

                texture.TexName = ReplaceWith;
                return true;
            }
        }
    }
}

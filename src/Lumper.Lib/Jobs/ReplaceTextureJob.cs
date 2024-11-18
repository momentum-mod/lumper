namespace Lumper.Lib.Jobs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Lumps.BspLumps;
using Lumper.Lib.Bsp.Struct;
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

        int counter = 0;
        foreach (TexData texture in texDataLump.Data)
        {
            string name = texture.TexName;
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

            // Compiled because we use this regex a ton of times, explicit capture since we
            // don't care about capture groups.
            // Fine if this throws, job will just fail.
            _regexMatcher = new Regex(
                Matcher,
                RegexOptions.Compiled | RegexOptions.ExplicitCapture,
                TimeSpan.FromSeconds(1)
            );
        }

        public bool TryReplace(TexData texture)
        {
            if (IsRegex)
            {
                if (_regexMatcher is null)
                    return false;

                string newName = _regexMatcher.Replace(texture.TexName, ReplaceWith);
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using SharpCompress.Archives.Zip;
using System.Linq;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using JsonSubTypes;
using Lumper.Tasks;
using Lumper.Lib.BSP;
using Lumper.Lib.BSP.Lumps;
using Lumper.Lib.BSP.Lumps.BspLumps;
using Lumper.Lib.BSP.Lumps.GameLumps;
using Lumper.Lib.BSP.Enum;
using Lumper.Lib.BSP.Struct;

namespace Lumper
{
    static class Program
    {
        //enum Bla { A, B, C };
        public static void Main(string[] args)
        {

            const bool compress = true;
            const bool change = true;
            const bool changePak = true;
            const bool compressPak = true;

            int pakFileIdx = 0;
            //DirectoryInfo di = new("./lumps/");
            //if (di.Exists)
            //    di.Delete(true);
            //di.Create();
            if (args.Length < 1)
            {
                Usage();
                Console.WriteLine("ERROR: No arguments were given.");
                return;
            }


            var map1 = new BspFile();
            map1.Load(args[0]);


            var texturesUtopia = new ChangeTextureTask()
            {
                Replace =
                {
                    {"TOOLS/TOOLSTRIGGER", "decals/graffiti_chicken"}
                },
                ReplaceRegex =
                {
                    { new KeyValuePair<Regex,string>(
                        new Regex(".*CONCRETEWALL011"),
                        "CS_ITALY/PWOOD1") },
                    { new KeyValuePair<Regex,string>(
                        new Regex("/COMPUTERWALL.*"),
                        "/COMPUTERWALL005") },
                    { new KeyValuePair<Regex,string>(
                        new Regex(".*CONCRETEWALL00.*"),
                        "GLASS/COMBINEGLASS001A") }
                }
            };
            string steamDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                        + "/.steam/steam/steamapps/common/";
            string momDir = steamDir + "Momentum Mod Playtest";
            string bspfixPath = momDir + "/bin/linux64/bspfix";
            string cssMountDir = steamDir + "Counter-Strike Source/cstrike";
            var tasks = new List<LumperTask>()
            {
                texturesUtopia,
                new StripperTask("./stripper/test.cfg"),
                //new StripperTask("./stripper/surf.cfg"),
                new StripperTask("./stripper/lumpertest.cfg"),
                //new StripperTask("./stripper/surf_rebel_scaz.cfg"),
                new RunExternalToolTask("cat",  "cat.bsp", "cat.bsp", true),
                new RunExternalToolTask("cat",  "cat.bsp", "cat.bsp", true),
                new RunExternalToolTask(bspfixPath,
                                        "bspfix_tmp.bsp -game \""+cssMountDir+"\"",
                                        "bspfix_tmp.bsp",
                                        "bspfix_tmp_fixed.bsp"),
                /*new RunExternalToolTask(bspfixPath,
                                        "bspfix_tmp.bsp -game \""+cssMountDir+"\"",
                                        "bspfix_tmp.bsp",
                                        "bspfix_tmp_fixed.bsp"),*/
                new CompressionTask(true, true)
            };
            using var fileStream = File.Open("bla.json", FileMode.Create, FileAccess.Write, FileShare.None);
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                //                serializer.Converters.Add(JsonSubtypesConverterBuilder
                //                    .Of(typeof(BspFileTask), "Type2") // type property is only defined here
                //                    .RegisterSubtype(typeof(ChangeTextureTask), Bla.A)
                //                    .RegisterSubtype(typeof(RunExternalToolTask), Bla.B)
                //                    .RegisterSubtype(typeof(StripperTask), Bla.C)
                //                    .SerializeDiscriminatorProperty() // ask to serialize the type property
                //                    .Build());
                using (StreamWriter sw = new StreamWriter(fileStream))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, tasks);
                }
            }
            tasks.Clear();

            //List<BspFileTask> tasks;
            using var fileStream2 = File.Open("bla.json", FileMode.Open, FileAccess.Read, FileShare.Read);
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                /*                serializer.Converters.Add(JsonSubtypesConverterBuilder
                                    .Of(typeof(BspFileTask), "Type2") // type property is only defined here
                                    .RegisterSubtype(typeof(ChangeTextureTask), Bla.A)
                                    .RegisterSubtype(typeof(RunExternalToolTask), Bla.B)
                                    .RegisterSubtype(typeof(StripperTask), Bla.C)
                                    .SerializeDiscriminatorProperty() // ask to serialize the type property
                                    .Build());*/
                using (var sr = new StreamReader(fileStream2))
                using (var reader = new JsonTextReader(sr))
                {
                    tasks = (List<LumperTask>)serializer.Deserialize<List<LumperTask>>(reader);
                }
            }

            /*Node node = node1;
            while (node != null)
            {
                node = node.Run();
            }*/

            foreach (var task in tasks)
            {
                task.Progress.OnPercentChanged += (sender, percent) => PrintProgress(percent);
                task.Run(map1);
                Console.Error.WriteLine();
            }





            //LoadMap(args[0]);

            var cubemapsLump = map1.GetLump(BspLumpType.Cubemaps);
            //cubemapsLump.Length = 0;

            if (change)
            {
                var rng = new Random(42);

                var pakFileLump = map1.GetLump<PakFileLump>();
                {
                    pakFileLump.DataStream.Seek(0, SeekOrigin.Begin);
                    using var pakFile = File.Open($"./pakfile{pakFileIdx++}.zip", FileMode.Create);
                    pakFileLump.DataStream.CopyTo(pakFile);
                }
                //var pakdir = new DirectoryInfo("./pakfile");
                //if (pakdir.Exists)
                //pakdir.Delete(true);
                using (ZipArchive zip1 = pakFileLump.GetZipArchive())
                {
                    //zip1.ExtractToDirectory(pakdir.FullName);
                    //pakdir.Create();
                    var delEntities = new List<ZipArchiveEntry>();
                    //var reader = zip1.ExtractAllEntries();
                    /*while (reader.MoveToNextEntry())
                    {
                        FileInfo fi = new(Path.Join(pakdir.FullName, reader.Entry.Key));
                        Directory.CreateDirectory(fi.Directory.FullName);
                        //var entStream = entry.OpenEntryStream();
                        using (FileStream fstream = new FileStream(fi.FullName, FileMode.Create))
                        {
                            //entStream.CopyTo(fstream);

                            reader.WriteEntryTo(fstream);


                            //zip1.CopyTo(fstream, (int)entry.CompressedSize);

                            //var buffer = new byte[entry.CompressedSize];
                            //zip1.Read(buffer, 0, buffer.Length);
                            //fstream.Write(buffer);


                        }
                    }
                    */
                    List<FileStream> fileStreams = new();
                    if (changePak)
                    {
                        foreach (var entry in zip1.Entries)
                        {
                            if (entry.Key.EndsWith("concretewall011.vtf"))
                            //if (entry.Name.StartsWith("concretewall")
                            //|| entry.Name.StartsWith("computerwall"))
                            //|| entry.Name.StartsWith("computerwall"))
                            {
                                delEntities.Add(entry);
                            }
                        }
                        int fileIdx = 0;
                        foreach (var entry in delEntities)
                        {
                            /*
                            string fname = pakDirKitsuneGrid.GetFiles().Where(x => x.Extension == ".vtf").ElementAt(fileIdx).FullName;
                            var fs = new FileStream(fname, FileMode.Open);
                            fileStreams.Add(fs);
                            zip1.AddEntry(entry.Key, fs);
                            var vmt = zip1.Entries.Where(x => x.Key.Substring(x.Key.Length - 4) == ".vmt").First();
                            fname = fname.Substring(0, fname.Length - 4) + ".vmt";
                            fname = pakDirKitsuneGrid.GetFiles().Where(x => x.FullName == fname).First().FullName;
                            fs = new FileStream(fname, FileMode.Open);
                            zip1.AddEntry(vmt.Key, fs);
                            zip1.RemoveEntry(vmt);
                            */
                            zip1.RemoveEntry(entry);
                            fileIdx++;
                        }
                    }

                    /*
                    foreach (var file in pakDirKitsuneGrid.GetFiles())
                    {
                        var fs = new FileStream(file.FullName, FileMode.Open);
                        fileStreams.Add(fs);
                        zip1.AddEntry("materials/grids/" + file.Name, fs);
                    }
                    */

                    pakFileLump.SetZipArchive(zip1, compressPak);
                    foreach (var fs in fileStreams)
                    {
                        fs.Dispose();
                    }
                }


                string model = "models/props/metal_box.mdl";
                //string model = "models/surf_sirius/whale2.mdl";
                //string model = "models/props/de_overpass/balloon.mdl";
                //string model = "models/props/de_train/hr_t/train_car_b/train_car_b.mdl";
                //string model = "models/weapons/mom_machinegun/w_mom_machinegun.mdl";
                //string model = "models/weapons/mom_plasmagun/w_mom_plasmagun.mdl";
                //string model = "models/weapons/mom_concgrenade/w_mom_concgrenade.mdl";
                //string model = "models/weapons/mom_knife/w_mom_knife.mdl";
                //string model = "models/weapons/mom_rocket/mom_rocket_jump.mdl";

                var entityLump = map1.GetLump<EntityLump>();
                foreach (var entity in entityLump.Data)
                {
                    Console.WriteLine($"ClassName: {entity.ClassName}");
                    foreach (var prop in entity.Properties)
                    {
                        Console.WriteLine($"\t{prop}");
                    }
                    foreach (var prop in entity.Properties)
                    {
                        //if(prop.Key == "rendercolor")
                        if (prop.Key == "_light" || prop.Key == "_ambient")
                        {
                            if (prop is Entity.Property<string> sprop)
                            {
                                //      sprop.Value = "0 255 0";
                                //sprop.Value = "0 0 255 255";
                            }
                        }
                        else if (entity.ClassName.StartsWith("light") && prop.Key == "origin")
                        {
                            if (prop is Entity.Property<string> sprop)
                            {
                                //sprop.Value = "0 0 0";
                            }
                        }
                        else if (entity.ClassName.StartsWith("env_sprite") && prop.Key == "model")
                        {

                            if (prop is Entity.Property<string> sprop)
                            {
                                //crashes if the model here doesn't exist?
                                //sprop.Value = "pacman/ghosts/ghost_blue.vmt";
                            }
                        }
                        else if (entity.ClassName.StartsWith("prop_physics") && prop.Key == "model")
                        {
                            if (prop is Entity.Property<string> sprop)
                            {
                                sprop.Value = model;
                            }
                        }
                        /*else if (entity.ClassName.StartsWith("prop_dynamic") && prop.Key == "model")
                        {
                            if (prop is Entity.Property<string> sprop)
                            {
                                if (!sprop.Value.Contains("bed_") && !sprop.Value.Contains("cover"))
                                    sprop.Value = model;
                            }
                        }*/
                        else if (entity.ClassName.StartsWith("prop_glados_core") && prop.Key == "model")
                        {
                            entity.ClassName = "prop_dynamic";
                            if (prop is Entity.Property<string> sprop)
                            {
                                sprop.Value = model;
                            }
                        }
                        else if (entity.ClassName.StartsWith("weapon_")
                        || (entity.ClassName.StartsWith("prop") && prop.Key == "model"))
                        {
                            var weapons = new string[] {
                                "weapon_momentum_pistol",
                                "weapon_momentum_shotgun",
                                "weapon_momentum_machinegun",
                                "weapon_momentum_sniper",
                                "weapon_momentum_grenade",
                                "weapon_momentum_concgrenade",
                                "weapon_knife",
                                "weapon_momentum_rocketlauncher",
                                "weapon_momentum_stickylauncher",
                                "weapon_momentum_df_plasmagun",
                                "weapon_momentum_df_rocketlauncher",
                                "weapon_momentum_df_bfg",
                                "weapon_momentum_df_grenadelauncher"
                            };
                            entity.ClassName = weapons[rng.Next(0, weapons.Length)];
                        }
                    }
                }
                var gameLump = map1.GetLump<GameLump>();
                var gameLumpSprp = gameLump.GetLump<Sprp>();
                if (gameLumpSprp is Sprp sprp)
                {
                    if (sprp.StaticProps.ActualVersion != StaticPropVersion.V12)
                        sprp.StaticProps.ActualVersion = StaticPropVersion.V11;
                    Console.WriteLine("StaticProps:");
                    for (int i = 0; i < sprp.StaticPropsDict.Data.Count; i++)
                    {
                        Console.WriteLine("\t" + sprp.StaticPropsDict.Data[i]);
                        //sprp.StaticPropsDict.Data[i] = "models/props/cs_office/Vending_machine.mdl";
                        sprp.StaticPropsDict.Data[i] = model;
                    }
                    for (int i = 0; i < sprp.StaticProps.Data.Count; i++)
                    {
                        sprp.StaticProps.Data[i].Skin = 1;
                        var test = sprp.StaticProps.Data[i].UniformScale;
                        if (model.Contains("train"))
                        {
                            sprp.StaticProps.Data[i].UniformScale = 0.5f;
                            sprp.StaticProps.Data[i].Solid = 0;
                        }
                        else
                            sprp.StaticProps.Data[i].UniformScale = (float)rng.Next(1, 8) * 0.5f;
                        //sprp.StaticProps.Data[i].UniformScale = 2.0f;
                        //sprp.StaticProps.Data[i].FadeMinDist = 100;
                        sprp.StaticProps.Data[i].Angle = new Angle()
                        {
                            Pitch = (float)rng.Next(0, 360),
                            Yaw = (float)90,
                            Roll = (float)45,
                        };
                        sprp.StaticProps.Data[i].DiffuseModulation = System.Drawing.Color.FromArgb
                        (
                            255,
                            0,
                            rng.Next(0, 255),
                            0
                        );
                    }
                    map1.Version = 21;
                }
            }

            if (compress)
            {
                int i2 = 0;
                foreach (var lump in map1.Lumps)
                {
                    Console.WriteLine($"{i2} {lump.Key} {lump.Value.GetType().Name}");
                    //if (i2 <= 35)
                    //    continue;

                    //if (i2 >= 15)
                    //if (i2 >= 23)
                    //if (i2 >= 23)
                    //    break;

                    i2++;
                    //if (lump.Value is UnmanagedLump)
                    if (lump.Value is not GameLump && lump.Value is not PakFileLump)
                    {
                        lump.Value.Compress = true;
                    }
                    else if (lump.Value is GameLump gameLump)
                    {
                        foreach (var lump2 in gameLump.Lumps)
                        {
                            lump2.Value.Compress = true;
                        }
                    }

                }
            }


            map1.Save("test.bsp");

            var map2 = new BspFile();
            map2.Load("test.bsp");
            var entityLump2 = map2.GetLump<EntityLump>();
            foreach (var entity in entityLump2.Data)
            {
                Console.WriteLine($"ClassName: {entity.ClassName}");
                foreach (var prop in entity.Properties)
                {
                    Console.WriteLine($"\t{prop}");
                }
            }

            var pakFileLump2 = map2.GetLump<PakFileLump>();
            {
                pakFileLump2.DataStream.Seek(0, SeekOrigin.Begin);
                using var pakFile = File.Open($"./pakfile{pakFileIdx++}.zip", FileMode.Create);
                pakFileLump2.DataStream.CopyTo(pakFile);
            }
        }

        private static BspFile LoadMap(string path)
        {
            try
            {
                var map = new BspFile();

                map.Load(path);

                return map;
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("ERROR: File {0} not found.", path);
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("ERROR: File {0} is not a valid Valve BSP.", path);
            }

            return null;
        }

        private static void Usage()
        {
            // TODO
        }
        static object printProgressLock = new();
        private static void PrintProgress(double percent)
        {
            if (Monitor.TryEnter(printProgressLock))
            {
                //Thread.Sleep(10);
                const int size = 10;
                int done = (int)(percent / size);
                int remaining = size - done;
                string progressBar = ""
                    .PadRight(done, '#')
                    .PadRight(size, '-');
                //console.error for now .. keeps it separate 
                Console.Error.Write($"\r{((int)percent).ToString().PadLeft(3)}% [{progressBar}]\b");
                Monitor.Enter(printProgressLock);
            }
        }
    }
}

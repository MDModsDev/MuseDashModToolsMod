using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using MelonLoader;
using MelonLoader.InternalUtils;
using Newtonsoft.Json;

namespace MuseDashModTools
{
    internal class Main : MelonMod
    {
        private const string ModLinks = "MDModsDev/ModLinks/dev/ModLinks.json";
        private static readonly List<LocalModInfo> ModInfos = new List<LocalModInfo>();
        private Version GameVersion { get; set; }


        public override void OnInitializeMelon()
        {
            ReadMods();
            CheckingLatestMods();
        }
#pragma warning disable S3885

        private void ReadMods()
        {
            GameVersion = new Version(UnityInformationHandler.GameVersion);
            var path = MelonHandler.ModsDirectory;
            var files = Directory.GetFiles(path, "*.dll");
            foreach (var file in files)
            {
                var mod = new LocalModInfo();
                var assembly = Assembly.LoadFrom(file);
                var attribute = MelonUtils.PullAttributeFromAssembly<MelonInfoAttribute>(assembly);
                mod.Name = attribute.Name;
                mod.Version = attribute.Version;
                mod.SHA256 = MelonUtils.ComputeSimpleSHA256Hash(file);
                ModInfos.Add(mod);
            }
        }

        private void CheckingLatestMods()
        {
            var webClient = new WebClient { Encoding = Encoding.UTF8 };
            string data;
            try
            {
                data = webClient.DownloadString("https://raw.githubusercontent.com/" + ModLinks);
            }
            catch (WebException)
            {
                data = webClient.DownloadString("https://raw.fastgit.org/" + ModLinks);
            }

            webClient.Dispose();

            var webModInfos = JsonConvert.DeserializeObject<List<WebModInfo>>(data);
            var loadedModNames = ModInfos.Select(x => x.Name).ToArray();
            foreach (var loadedMod in ModInfos)
            {
                var webModIdx = webModInfos.FindIndex(x => x.Name == loadedMod.Name);
                MelonLogger.Msg("------------------------------");
                if (webModIdx == -1)
                {
                    MelonLogger.Warning($"The mod \"{loadedMod.Name}\" isn't tracked.");
                    continue;
                }

                var storedMod = webModInfos[webModIdx];
                var comparison = new Version(loadedMod.Version).CompareTo(new Version(storedMod.Version));

                if (comparison > 0)
                {
                    MelonLogger.Msg($"WOW {loadedMod.Name} MOD CREATOR");
                }
                else if (comparison == 0)
                {
                    if (loadedMod.SHA256 != storedMod.SHA256)
                    {
                        MelonLogger.Warning($"The mod \"{loadedMod.Name}\" doesn't match what we have stored, may be modified. Proceed with caution.");
                    }
                    else
                    {
                        MelonLogger.Msg($"The mod \"{loadedMod.Name}\" is up-to-date");
                    }
                }
                else
                {
                    MelonLogger.Warning($"You are using an outdated version of \"{loadedMod.Name}\", please update the mod (if your game is downgraded, ignore this message)");
                }

                var gameVersionCompatible = false;
                if (comparison == 0)
                {
                    foreach (var compatibleVersion in storedMod.GameVersion)
                    {
                        var version = compatibleVersion == "*" ? GameVersion : new Version(compatibleVersion);
                        if (GameVersion.CompareTo(version) == 0)
                        {
                            gameVersionCompatible = true;
                        }
                    }

                    if (!gameVersionCompatible)
                    {
                        MelonLogger.Error($"The mod \"{loadedMod.Name}\" isn't compatible with game version {GameVersion}");
                        MelonLogger.Error("Supported versions: " + string.Join(", ", storedMod.GameVersion));
                    }
                }

                if (comparison <= 0)
                {
                    foreach (var incompatibleMod in storedMod.IncompatibleMods.Where(x => loadedModNames.Contains(x)))
                    {
                        MelonLogger.Error($"The mod \"{loadedMod.Name}\" isn't compatible with mod {incompatibleMod}");
                    }
                }
            }
        }
    }

    public class WebModInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string DownloadLink { get; set; }
        public string HomePage { get; set; }
        public string[] GameVersion { get; set; }
        public string Description { get; set; }
        public string[] DependentMods { get; set; }
        public string[] DependentLibs { get; set; }
        public string[] IncompatibleMods { get; set; }
        public string SHA256 { get; set; }
    }

    public class LocalModInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string SHA256 { get; set; }
    }
}
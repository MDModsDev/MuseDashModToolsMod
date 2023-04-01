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
        private const string ModLinks = "MDModsDev/ModLinks/main/ModLinks.json";
        private static readonly List<ModInfo> LocalMods = new List<ModInfo>();
        private string UIPath { get; set; }
        private string GameVersion { get; set; }

        public override void OnInitializeMelon()
        {
            ReadConfig();
            ReadMods();
            CheckingLatestMods();
        }

        private void ReadConfig()
        {
            var configPath = Path.Combine("UserData", "MuseDashModTools.cfg");
            if (File.Exists(configPath))
                UIPath = File.ReadAllText(configPath);
        }

        private void ReadMods()
        {
            GameVersion = UnityInformationHandler.GameVersion;
            var path = MelonHandler.ModsDirectory;
            var enabledModFiles = Directory.GetFiles(path, "*.dll");
            foreach (var file in enabledModFiles)
            {
                var mod = new ModInfo();
                try
                {
                    var assembly = Assembly.Load(File.ReadAllBytes(file));
                    var attribute = MelonUtils.PullAttributeFromAssembly<MelonInfoAttribute>(assembly);
                    mod.Name = attribute.Name;
                    mod.Version = attribute.Version;
                    mod.SHA256 = MelonUtils.ComputeSimpleSHA256Hash(file);
                    LocalMods.Add(mod);
                }
                catch
                {
                    // Ignore
                }
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

            var webMods = JsonConvert.DeserializeObject<List<ModInfo>>(data);

            foreach (var localMod in LocalMods)
            {
                var webMod = webMods.FirstOrDefault(x => x.Name == localMod.Name);
                if (webMod == null)
                {
                    MelonLogger.Warning($"The mod \"{localMod.Name}\" isn't tracked.");
                    continue;
                }

                if (!webMod.GameVersion.Any(x => x == "*" || x == GameVersion))
                {
                    MelonLogger.Error($"The mod \"{localMod.Name}\" isn't compatible with game version {GameVersion}");
                    MelonLogger.Error("Supported versions: " + string.Join(", ", webMod.GameVersion));
                }

                var comparison = new Version(localMod.Version).CompareTo(new Version(webMod.Version));
                if (comparison > 0)
                {
                    MelonLogger.Msg($"WOW {localMod.Name} MOD CREATOR");
                }
                else if (comparison == 0)
                {
                    if (localMod.SHA256 != webMod.SHA256)
                    {
                        MelonLogger.Warning($"The mod \"{localMod.Name}\" doesn't match what we have stored, may be modified. Proceed with caution.");
                    }
                    else
                    {
                        MelonLogger.Msg($"The mod \"{localMod.Name}\" is up-to-date");
                    }
                }
                else
                {
                    MelonLogger.Warning($"You are using an outdated version of \"{localMod.Name}\", please update the mod (if your game is downgraded, ignore this message)");
                }

                if (comparison <= 0)
                {
                    foreach (var incompatibleMod in webMod.IncompatibleMods.Where(x => LocalMods.Select(y => y.Name).Contains(x)))
                    {
                        MelonLogger.Error($"The mod \"{localMod.Name}\" isn't compatible with mod {incompatibleMod}");
                    }
                }
            }
        }
    }

    public class ModInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string FileName { get; set; }
        public string DownloadLink { get; set; }
        public string HomePage { get; set; }
        public string[] GameVersion { get; set; }
        public string Description { get; set; }
        public string[] DependentMods { get; set; }
        public string[] DependentLibs { get; set; }
        public string[] IncompatibleMods { get; set; }
        public string SHA256 { get; set; }
    }
}
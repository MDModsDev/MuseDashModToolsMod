using Il2CppSystem;
using MelonLoader;
using MelonLoader.InternalUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace MuseDashModTools
{
    internal class Main : MelonMod
    {
        private static System.Version GameVersion { get; set; }
        private static List<ModsInfo> modsInfos = new List<ModsInfo>();
        private const string ModLinks = "https://raw.githubusercontent.com/MDModsDev/ModLinks/dev/ModLinks.json";

        public override void OnInitializeMelon()
        {
            ReadMods();
            CheckingLatestMods();
        }

        private void ReadMods()
        {
            GameVersion = new System.Version(UnityInformationHandler.GameVersion);
            string path = MelonHandler.ModsDirectory;
            string[] files = Directory.GetFiles(path, "*.dll");
            foreach (var file in files)
            {
                var mod = new ModsInfo();
                var assembly = Assembly.LoadFrom(file);
                PropertyInfo[] properties = assembly.GetCustomAttribute(typeof(MelonInfoAttribute)).GetType().GetProperties();
                foreach (var property in properties)
                {
                    if (property.Name == "Name")
                    {
                        mod.Name = property.GetValue(assembly.GetCustomAttribute(typeof(MelonInfoAttribute)), null).ToString();
                    }
                    if (property.Name == "Version")
                    {
                        mod.Version = property.GetValue(assembly.GetCustomAttribute(typeof(MelonInfoAttribute)), null).ToString();
                    }
                }
                modsInfos.Add(mod);
            }
        }

        private void CheckingLatestMods()
        {
            var webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            string Datas = Encoding.Default.GetString(webClient.DownloadData(ModLinks));
            var WebModsInfo = JsonConvert.DeserializeObject<Dictionary<string, ModsInfo>>(Datas);
            foreach (var loadedMod in modsInfos)
            {
                if (!WebModsInfo.ContainsKey(loadedMod.Name))
                {
                    MelonLogger.Warning($"The mod \"{loadedMod.Name}\" isn't tracked.");
                    continue;
                }
                ModsInfo storedMod = WebModsInfo[loadedMod.Name];
                int comparison;
                var supportedVersions = new System.Version[storedMod.GameVersion.Length];
                bool result = false;
                for (int i = 0; i < storedMod.GameVersion.Length; i++)
                {
                    var version = storedMod.GameVersion[i] == "*" ? GameVersion : new System.Version(storedMod.GameVersion[i]);
                    int t = GameVersion.CompareTo(version);
                    if (t == 0)
                    {
                        result = true;
                    }
                    supportedVersions[t] = version;
                }
                if (!result)
                {
                    MelonLogger.Error($"The mod \"{loadedMod.Name}\" isn't compatible with game version {GameVersion}");
                    MelonLogger.Error("Supported versions: " + string.Join(", ", storedMod.GameVersion));
                    continue;
                }
                comparison = new System.Version(loadedMod.Version).CompareTo(new System.Version(storedMod.Version));
                if (comparison < 0)
                {
                    MelonLogger.Warning($"You are using an outdated version of \"{loadedMod.Name}\", please update the mod");
                }
                else if (comparison == 0)
                {
                    MelonLogger.Msg($"the mod \"{loadedMod.Name}\" is up-to-date");
                }
                else
                {
                    MelonLogger.Msg($"WOW {loadedMod.Name.ToUpper()} MOD CREATER");
                }

            }
        }
    }

    public class ModsInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string DownloadLink { get; set; }
        public string[] GameVersion { get; set; }
        public string Description { get; set; }
        public string[] DependentMods { get; set; }
        public string SHA256 { get; set; }

        public ModsInfo()
        {
            Name = "Unknown";
            Version = "Unknown";
            Author = "Unknown";
            DownloadLink = "";
            GameVersion = new string[0];
            Description = "";
            DependentMods = new string[0];
            SHA256 = "";
        }

        public ModsInfo(string name, string version, string author, string downloadLink, string[] gameVersion, string description, string[] dependentMods, string sha256)
        {
            Name = name;
            Version = version;
            Author = author;
            DownloadLink = downloadLink;
            GameVersion = gameVersion;
            Description = description;
            DependentMods = dependentMods;
            SHA256 = sha256;
        }
    }
}
using MelonLoader;
using MelonLoader.InternalUtils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace MuseDashModTools
{
    internal class Main : MelonMod
    {
        private static int GameVersion { get; set; }
        private static List<ModsInfo> modsInfos = new List<ModsInfo>();
        private const string ModLinks = "https://raw.githubusercontent.com/MDModsDev/ModLinks/main/ModLinks.json";

        public override void OnInitializeMelon()
        {
            ReadMods();
            CheckingLatestMods();
        }

        private void ReadMods()
        {
            GameVersion = int.Parse(UnityInformationHandler.GameVersion.Replace(".", ""));
            string path = MelonHandler.ModsDirectory;
            var files = Directory.GetFiles(path, "*.dll");
            foreach (var file in files)
            {
                var mod = new ModsInfo();
                var assembly = Assembly.LoadFrom(file);
                var properties = assembly.GetCustomAttribute(typeof(MelonInfoAttribute)).GetType().GetProperties();
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
            var webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;
            var Datas = Encoding.Default.GetString(webClient.DownloadData(ModLinks));
            var WebModsInfo = JsonConvert.DeserializeObject<List<ModsInfo>>(Datas);
            for (int i = 0; i < modsInfos.Count; i++)
            {
                for (int j = 0; j < WebModsInfo.Count; j++)
                {
                    int ModVersion = int.Parse(modsInfos[i].Version.Replace(".", ""));
                    int WebModVersion = int.Parse(WebModsInfo[j].Version.Replace(".", ""));
                    int WebModGameVersion = int.Parse(WebModsInfo[j].GameVersion.Replace(".", ""));
                    if (modsInfos[i].Name == WebModsInfo[j].Name)
                    {
                        if (ModVersion == WebModVersion)
                        {
                            if (GameVersion == WebModGameVersion)
                            {
                                MelonLogger.Msg(modsInfos[i].Name + " is the latest version");
                                break;
                            }
                            else if (GameVersion > WebModGameVersion)
                            {
                                MelonLogger.Msg("Please downgrade the game to make " + modsInfos[i].Name + " to work");
                                break;
                            }
                            else if (GameVersion < WebModGameVersion)
                            {
                                MelonLogger.Msg("Are you using a pirated game or you forget to upgrade the game? " + modsInfos[i].Name + " maybe not working on this game version");
                                break;
                            }
                        }
                        else if (ModVersion > WebModGameVersion)
                        {
                            MelonLogger.Msg("WOW MOD CREATER");
                            break;
                        }
                        else if (ModVersion < WebModGameVersion)
                        {
                            MelonLogger.Msg("You are using an outdated version of " + modsInfos[i].Name + ", please update the mod");
                            break;
                        }
                    }
                    else if (modsInfos[i].Name != WebModsInfo[j].Name && j == WebModsInfo.Count - 1)
                    {
                        MelonLogger.Msg("Cannot find your mod " + modsInfos[i].Name + " in modlinks, are u trying to create a new mod?");
                        break;
                    }
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
        public string GameVersion { get; set; }
        public string Description { get; set; }
        public string[] DependentMods { get; set; }
        public string SHA256 { get; set; }

        public ModsInfo()
        {
            Name = "Unknown";
            Version = "Unknown";
            Author = "Unknown";
            DownloadLink = "";
            GameVersion = "Unknown";
            Description = "";
            DependentMods = new string[0];
            SHA256 = "";
        }

        public ModsInfo(string name, string version, string author, string downloadLink, string gameVersion, string description, string[] dependentMods, string sha256)
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
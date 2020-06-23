using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PodcastSyncLib
{
    public class Settings
    {
        static Settings()
        {
            SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rrelyea\\PodcastSync");
            SettingsFile = Path.Combine(SettingsDirectory, "settings.json");
        }

        public static string SettingsDirectory { get; set; }
        public static string SettingsFile { get; set; }
        public static Settings Instance { get; set; }

        public Settings()
        {
            Podcasts = new ObservableCollection<Podcast>();
        }

        public ObservableCollection<Podcast> Podcasts { get; set; }
        public string PodcastPath { get; set; }
        public double? WindowTop { get; set; }
        public double? WindowLeft { get; set; }
        public double? WindowHeight { get; set; }
        public double? WindowWidth { get; set; }
        public static bool Dirty { get; set; }

        public static async Task Load()
        {
            if (File.Exists(SettingsFile))
            {
                using (FileStream fs = File.OpenRead(SettingsFile))
                {
                    Instance = await JsonSerializer.DeserializeAsync<Settings>(fs);
                }
            }

            if (Instance == null)
            {
                Instance = new Settings();
            }
        }

        public static async void Save(bool forceSave = false)
        {
            if (forceSave || Settings.Dirty)
            {
                Directory.CreateDirectory(SettingsDirectory);
                using (FileStream fs = File.Create(SettingsFile))
                {
                    var options = new JsonSerializerOptions() { WriteIndented = true };
                    await JsonSerializer.SerializeAsync(fs, Instance, options);
                }
            }
        }
    }
}

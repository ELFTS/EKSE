using System;
using System.IO;
using Newtonsoft.Json;

namespace EKSE.Services
{
    public sealed class SettingsManager
    {
        private static readonly Lazy<SettingsManager> _instance = new(() => new SettingsManager());
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;

        public static SettingsManager Instance => _instance.Value;

        public SettingsManager()
        {
            _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            _currentSettings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null) return _currentSettings = settings;
                }
            }
            catch { }
            return _currentSettings = CreateDefaultSettings();
        }

        public void SaveSettings(AppSettings? settings = null)
        {
            try
            {
                settings ??= _currentSettings;
                var dir = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                _currentSettings = settings;
            }
            catch { }
        }

        public AppSettings GetCurrentSettings() => _currentSettings;

        private static AppSettings CreateDefaultSettings() => new()
        {
            AutoStart = false,
            MinimizeToTray = false,
            EnableSound = true,
            Volume = 80
        };
    }

    public class AppSettings
    {
        public bool AutoStart { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool EnableSound { get; set; } = true;
        public int Volume { get; set; } = 80;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using System.Windows.Input;
using EKSE.Models;

namespace EKSE.Services
{
    public class ProfileManager
    {
        private readonly string _profilesDirectory;
        private readonly List<SoundProfile> _profiles = new();
        private SoundProfile? _currentProfile;

        public event EventHandler? ProfilesChanged;
        public event EventHandler? CurrentProfileChanged;

        private static readonly Dictionary<string, Key> KeyMap = new()
        {
            ["Space"] = Key.Space, ["Enter"] = Key.Enter, ["Backspace"] = Key.Back,
            ["Tab"] = Key.Tab, ["Caps"] = Key.CapsLock, ["Esc"] = Key.Escape,
            ["Win"] = Key.LWin, ["L Shift"] = Key.LeftShift, ["R Shift"] = Key.RightShift,
            ["L Ctrl"] = Key.LeftCtrl, ["R Ctrl"] = Key.RightCtrl, ["L Alt"] = Key.LeftAlt,
            ["R Alt"] = Key.RightAlt, ["↑"] = Key.Up, ["↓"] = Key.Down,
            ["←"] = Key.Left, ["→"] = Key.Right
        };

        public ProfileManager()
        {
            _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
            Directory.CreateDirectory(_profilesDirectory);

            LoadProfiles();
            if (!_profiles.Any()) CreateDefaultProfile();

            _currentProfile = _profiles.FirstOrDefault() ?? CreateDefaultProfile();
            
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
            CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyList<SoundProfile> Profiles => _profiles.AsReadOnly();
        public SoundProfile? CurrentProfile => _currentProfile;

        private JsonSerializerSettings GetJsonSettings() => new()
        {
            Converters = { new SoundProfileJsonConverter() },
            Formatting = Formatting.Indented
        };

        private void LoadProfiles()
        {
            foreach (var directory in Directory.GetDirectories(_profilesDirectory))
            {
                try
                {
                    var profile = LoadProfileFromDirectory(directory);
                    if (profile != null) _profiles.Add(profile);
                }
                catch { }
            }
        }

        private SoundProfile? LoadProfileFromDirectory(string directory)
        {
            var profileName = Path.GetFileName(directory);
            var configFile = Path.Combine(directory, "index.json");

            SoundProfile profile;
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                profile = JsonConvert.DeserializeObject<SoundProfile>(json, GetJsonSettings()) ?? new SoundProfile(profileName);
            }
            else
            {
                profile = new SoundProfile(profileName);
            }

            profile.FilePath = directory;
            UpdateKeySoundsFromAssignments(profile);
            return profile;
        }

        private void UpdateKeySoundsFromAssignments(SoundProfile profile)
        {
            profile.KeySounds.Clear();
            if (profile.AssignedSounds?.Count > 0)
            {
                foreach (var assignment in profile.AssignedSounds.Where(a => a != null))
                {
                    var key = ParseKeyName(assignment.Key);
                    if (key.HasValue && !string.IsNullOrEmpty(assignment.Sound))
                    {
                        var soundPath = Path.Combine(profile.FilePath!, "sounds", assignment.Sound);
                        if (File.Exists(soundPath)) profile.KeySounds[key.Value] = soundPath;
                    }
                }
            }
        }

        private Key? ParseKeyName(string? keyName)
        {
            if (string.IsNullOrEmpty(keyName)) return null;

            // 数字键处理
            if (keyName.Length == 1 && char.IsDigit(keyName[0]))
                return (Key)Enum.Parse(typeof(Key), $"D{keyName[0]}");

            if (Enum.TryParse<Key>(keyName, true, out var key)) return key;
            return KeyMap.TryGetValue(keyName, out var mappedKey) ? mappedKey : null;
        }

        private SoundProfile CreateDefaultProfile()
        {
            var profile = new SoundProfile("默认方案");
            var directory = Path.Combine(_profilesDirectory, profile.Name);
            Directory.CreateDirectory(directory);
            profile.FilePath = directory;

            _currentProfile?.KeySounds.ToList().ForEach(kvp => profile.KeySounds[kvp.Key] = kvp.Value);
            _profiles.Add(profile);
            SaveProfile(profile);
            return profile;
        }

        public SoundProfile CreateProfile(string name)
        {
            var profile = new SoundProfile(name);
            var directory = Path.Combine(_profilesDirectory, name);
            Directory.CreateDirectory(directory);
            profile.FilePath = directory;

            _currentProfile?.KeySounds.ToList().ForEach(kvp => profile.KeySounds[kvp.Key] = kvp.Value);
            _profiles.Add(profile);
            SaveProfile(profile);
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
            return profile;
        }

        public void DeleteProfile(SoundProfile profile)
        {
            if (_profiles.Count <= 1 || profile.Name == "默认方案" || !_profiles.Remove(profile))
                return;

            if (_currentProfile == profile)
            {
                _currentProfile = _profiles.FirstOrDefault();
                CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
            }

            try { if (Directory.Exists(profile.FilePath)) Directory.Delete(profile.FilePath, true); }
            catch { }

            ProfilesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SaveProfile(SoundProfile profile)
        {
            try
            {
                if (string.IsNullOrEmpty(profile.FilePath)) return;

                profile.AssignedSounds = profile.KeySounds.Select(kvp => new SoundAssignment
                {
                    Key = kvp.Key.ToString(),
                    Sound = Path.GetFileName(kvp.Value)
                }).ToList();

                var json = JsonConvert.SerializeObject(profile, GetJsonSettings());
                File.WriteAllText(Path.Combine(profile.FilePath, "index.json"), json);
            }
            catch { }
        }

        public void SwitchProfile(SoundProfile profile)
        {
            if (!_profiles.Contains(profile)) return;

            _currentProfile?.KeySounds.Clear();
            _currentProfile = profile;
            UpdateKeySoundsFromAssignments(_currentProfile);
            CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetKeySound(Key key, string soundPath)
        {
            if (_currentProfile == null || !File.Exists(soundPath)) return;
            _currentProfile.KeySounds[key] = soundPath;
            SaveProfile(_currentProfile);
        }

        public string? GetKeySound(Key key)
        {
            if (_currentProfile?.KeySounds.TryGetValue(key, out var path) == true && File.Exists(path))
                return path;
            return null;
        }

        public string? ImportSound(string sourcePath)
        {
            if (_currentProfile == null || !File.Exists(sourcePath)) return null;

            var soundsDir = Path.Combine(_currentProfile.FilePath!, "sounds");
            Directory.CreateDirectory(soundsDir);

            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(soundsDir, fileName);

            for (int i = 1; File.Exists(destPath); i++)
                destPath = Path.Combine(soundsDir, $"{Path.GetFileNameWithoutExtension(fileName)}_{i}{Path.GetExtension(fileName)}");

            File.Copy(sourcePath, destPath);
            SaveProfile(_currentProfile);
            return destPath;
        }

        public SoundProfile? ImportProfile(string importPath)
        {
            if (!File.Exists(importPath)) return null;

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                ZipFile.ExtractToDirectory(importPath, tempDir);
                var profile = LoadProfileFromDirectory(tempDir);
                if (profile == null) return null;

                // 处理重名
                var targetDir = Path.Combine(_profilesDirectory, profile.Name);
                if (Directory.Exists(targetDir))
                {
                    profile.Name = $"{profile.Name}_{DateTime.Now:yyyyMMddHHmmss}";
                    targetDir = Path.Combine(_profilesDirectory, profile.Name);
                }

                Directory.CreateDirectory(targetDir);
                CopyDirectory(tempDir, targetDir);
                profile.FilePath = targetDir;

                _profiles.Add(profile);
                SwitchProfile(profile);
                ProfilesChanged?.Invoke(this, EventArgs.Empty);
                return profile;
            }
            catch { return null; }
            finally { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
        }

        public bool ExportProfile(SoundProfile profile, string exportPath)
        {
            try
            {
                if (Directory.Exists(profile.FilePath))
                {
                    ZipFile.CreateFromDirectory(profile.FilePath, exportPath);
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }

        public bool RenameProfile(SoundProfile? profile, string newName)
        {
            if (profile == null || string.IsNullOrWhiteSpace(newName) || string.IsNullOrEmpty(profile.FilePath))
                return false;

            if (_profiles.Any(p => p != profile && p.Name?.Equals(newName, StringComparison.OrdinalIgnoreCase) == true))
                return false;

            var oldPath = profile.FilePath;
            var newPath = Path.Combine(_profilesDirectory, newName);

            try
            {
                var wasCurrent = _currentProfile == profile;
                if (wasCurrent) _currentProfile?.KeySounds.Clear();

                if (Directory.Exists(newPath)) Directory.Delete(newPath, true);
                if (Directory.Exists(oldPath)) Directory.Move(oldPath, newPath);

                profile.Name = newName;
                profile.FilePath = newPath;
                SaveProfile(profile);

                if (wasCurrent)
                {
                    _currentProfile = profile;
                    CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
                }

                ProfilesChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

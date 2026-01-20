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
    /// <summary>
    /// 声音方案管理器
    /// </summary>
    public class ProfileManager
    {
        private readonly string _profilesDirectory;
        private readonly List<SoundProfile> _profiles;
        private SoundProfile? _currentProfile;
        
        // 事件：方案列表变化、当前方案变化
        public event EventHandler? ProfilesChanged;
        public event EventHandler? CurrentProfileChanged;
        
        // 特殊键名映射
        private static readonly Dictionary<string, Key> KeyMap = new Dictionary<string, Key>
        {
            ["Space"] = Key.Space, ["Enter"] = Key.Enter, ["Backspace"] = Key.Back,
            ["Tab"] = Key.Tab, ["Caps"] = Key.CapsLock, ["Esc"] = Key.Escape,
            ["Win"] = Key.LWin, ["L Shift"] = Key.LeftShift, ["R Shift"] = Key.RightShift,
            ["L Ctrl"] = Key.LeftCtrl, ["R Ctrl"] = Key.RightCtrl, ["L Alt"] = Key.LeftAlt,
            ["R Alt"] = Key.RightAlt, ["↑"] = Key.Up, ["↓"] = Key.Down,
            ["←"] = Key.Left, ["→"] = Key.Right, ["[ {"] = Key.OemOpenBrackets,
            ["] }"] = Key.OemCloseBrackets, ["; :"] = Key.OemSemicolon, ["'"] = Key.OemQuotes,
            [", <"] = Key.OemComma, [". >"] = Key.OemPeriod, ["/ ?"] = Key.OemQuestion,
            ["\\"] = Key.Oem5, ["-"] = Key.OemMinus, ["="] = Key.OemPlus,
            ["Del"] = Key.Delete, ["Ins"] = Key.Insert, ["Home"] = Key.Home,
            ["End"] = Key.End, ["PgUp"] = Key.PageUp, ["PgDn"] = Key.PageDown,
            ["Pause"] = Key.Pause, ["SrcLk"] = Key.Scroll, ["Fn"] = Key.None
        };

        public ProfileManager()
        {
            _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
            _profiles = new List<SoundProfile>();
            
            if (!Directory.Exists(_profilesDirectory))
                Directory.CreateDirectory(_profilesDirectory);
            
            LoadProfiles();
            
            if (!_profiles.Any())
                CreateDefaultProfile();
            
            _currentProfile = _profiles.FirstOrDefault();
            if (_currentProfile == null)
            {
                CreateDefaultProfile();
                _currentProfile = _profiles.FirstOrDefault();
            }
            
            // 触发事件以通知监听者
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
            CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
        }
        
        public IReadOnlyList<SoundProfile> Profiles => _profiles.AsReadOnly();
        public SoundProfile? CurrentProfile => _currentProfile;
        
        private void LoadProfiles()
        {
            try
            {
                var profileDirectories = Directory.GetDirectories(_profilesDirectory);
                foreach (var directory in profileDirectories)
                {
                    try
                    {
                        var profileName = Path.GetFileName(directory);
                        var configFile = Path.Combine(directory, "index.json");
                        
                        SoundProfile profile;
                        if (File.Exists(configFile))
                        {
                            var json = File.ReadAllText(configFile);
                            var settings = new JsonSerializerSettings();
                            settings.Converters.Add(new SoundProfileJsonConverter());
                            var deserializedProfile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
                            if (deserializedProfile != null)
                            {
                                profile = deserializedProfile;
                                profile.FilePath = directory;
                                UpdateKeySoundsFromAssignments(profile);
                            }
                            else
                            {
                                profile = new SoundProfile(profileName)
                                {
                                    FilePath = directory
                                };
                            }
                        }
                        else
                        {
                            profile = new SoundProfile(profileName)
                            {
                                FilePath = directory
                            };
                        }
                        
                        _profiles.Add(profile);
                    }
                    catch
                    {
                        // 忽略单个方案加载错误
                    }
                }
            }
            catch
            {
            }
        }
        
        private void UpdateKeySoundsFromAssignments(SoundProfile profile)
        {
            profile.KeySounds.Clear();
            if (profile.AssignedSounds?.Count > 0)
            {
                foreach (var assignment in profile.AssignedSounds)
                {
                    if (assignment != null && !string.IsNullOrEmpty(profile.FilePath) && !string.IsNullOrEmpty(assignment.Key) && !string.IsNullOrEmpty(assignment.Sound))
                    {
                        var key = ParseKeyName(assignment.Key);
                        if (key.HasValue)
                        {
                            var soundPath = Path.Combine(profile.FilePath, "sounds", assignment.Sound);
                            if (File.Exists(soundPath))
                            {
                                profile.KeySounds[key.Value] = soundPath;
                            }
                        }
                    }
                }
            }
        }
        
        private Key? ParseKeyName(string? keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return null;
            
            // 处理数字键
            if (keyName.Length == 1 && char.IsDigit(keyName[0]))
            {
                return keyName[0] switch
                {
                    '0' => Key.D0, '1' => Key.D1, '2' => Key.D2, '3' => Key.D3,
                    '4' => Key.D4, '5' => Key.D5, '6' => Key.D6, '7' => Key.D7,
                    '8' => Key.D8, '9' => Key.D9, _ => null
                };
            }
            
            // 处理D开头的数字键（如D0, D1）
            if (keyName.Length == 2 && keyName.StartsWith("D") && char.IsDigit(keyName[1]))
            {
                return keyName[1] switch
                {
                    '0' => Key.D0, '1' => Key.D1, '2' => Key.D2, '3' => Key.D3,
                    '4' => Key.D4, '5' => Key.D5, '6' => Key.D6, '7' => Key.D7,
                    '8' => Key.D8, '9' => Key.D9, _ => null
                };
            }
            
            // 尝试直接解析Key枚举
            if (Enum.TryParse<Key>(keyName, true, out var key))
                return key;
            
            // 尝试从映射表中查找
            return KeyMap.TryGetValue(keyName, out var mappedKey) ? mappedKey : null;
        }
        
        private void CreateDefaultProfile()
        {
            var defaultProfile = new SoundProfile("默认方案");
            var profileDirectory = Path.Combine(_profilesDirectory, defaultProfile.Name);
            
            Directory.CreateDirectory(profileDirectory);
            defaultProfile.FilePath = profileDirectory;
            
            CopyCurrentProfileMappings(defaultProfile);
            
            _profiles.Add(defaultProfile);
            SaveProfile(defaultProfile);
        }
        
        public SoundProfile CreateProfile(string name)
        {
            var profile = new SoundProfile(name);
            var profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
            
            Directory.CreateDirectory(profileDirectory);
            profile.FilePath = profileDirectory;
            
            CopyCurrentProfileMappings(profile);
            
            _profiles.Add(profile);
            SaveProfile(profile);
            
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
            
            return profile;
        }
        
        private void CopyCurrentProfileMappings(SoundProfile targetProfile)
        {
            if (_currentProfile == null) return;
            
            foreach (var kvp in _currentProfile.KeySounds)
            {
                targetProfile.KeySounds[kvp.Key] = kvp.Value;
            }
        }
        
        public void DeleteProfile(SoundProfile profile)
        {
            // 至少需要保留一个方案
            if (_profiles.Count <= 1)
            {
                // 至少保留一个声音方案
                return;
            }
            
            // 不允许删除默认方案
            if (profile.Name == "默认方案")
            {
                // 关键配置保护：禁止删除默认方案
                return;
            }
            
            // 从方案列表中移除
            if (!_profiles.Remove(profile))
                return;
            
            // 如果删除的是当前方案，则切换到第一个可用方案
            if (_currentProfile == profile)
            {
                _currentProfile = _profiles.FirstOrDefault();
                // 触发当前方案改变事件，通知其他组件释放资源
                CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
            }
            
            // 删除物理文件和目录
            DeleteProfileDirectory(profile.FilePath);
            
            // 触发方案列表变化事件，通知UI更新
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// 删除方案目录及其内容
        /// </summary>
        /// <param name="profilePath">方案目录路径</param>
        private void DeleteProfileDirectory(string? profilePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(profilePath) && Directory.Exists(profilePath))
                {
                    // 使用更简单可靠的方式删除整个目录树
                    Directory.Delete(profilePath, true);
                }
            }
            catch
            {
                // 静默忽略删除失败，不进行重试或强制删除
            }
        }
        
        public void SaveProfile(SoundProfile profile)
        {
            try
            {
                if (!string.IsNullOrEmpty(profile.FilePath))
                {
                    var profileFile = Path.Combine(profile.FilePath, "index.json");
                    ConvertKeySoundsToAssignedSounds(profile);
                    
                    var settings = new JsonSerializerSettings 
                    { 
                        Formatting = Formatting.Indented
                    };
                    settings.Converters.Add(new SoundProfileJsonConverter());
                    
                    var json = JsonConvert.SerializeObject(profile, settings);
                    File.WriteAllText(profileFile, json);
                }
            }
            catch
            {
            }
        }
        
        private void ConvertKeySoundsToAssignedSounds(SoundProfile profile)
        {
            profile.AssignedSounds = new List<SoundAssignment>();
            foreach (var kvp in profile.KeySounds)
            {
                var soundFileName = Path.GetFileName(kvp.Value);
                if (!string.IsNullOrEmpty(soundFileName))
                {
                    profile.AssignedSounds.Add(new SoundAssignment
                    {
                        Key = kvp.Key.ToString(),
                        Sound = soundFileName
                    });
                }
            }
        }
        
        public void SetCurrentProfile(SoundProfile profile)
        {
            if (_profiles.Contains(profile))
            {
                _currentProfile = profile;
                CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public void SwitchProfile(SoundProfile profile)
        {
            if (profile != null && _profiles.Contains(profile))
            {
                // 如果当前有方案，先清理其资源
                if (_currentProfile != null)
                {
                    _currentProfile.KeySounds.Clear();
                }
                
                _currentProfile = profile;
                UpdateKeySoundsFromAssignments(_currentProfile);
                CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public void SetKeySound(Key key, string soundPath)
        {
            if (_currentProfile != null && File.Exists(soundPath))
            {
                try
                {
                    _currentProfile.KeySounds[key] = soundPath;
                    SaveProfile(_currentProfile);
                }
                catch
                {
                }
            }
        }
        
        public string? GetKeySound(Key key)
        {
            if (_currentProfile != null && _currentProfile.KeySounds.ContainsKey(key))
            {
                var soundPath = _currentProfile.KeySounds[key];
                return !string.IsNullOrEmpty(soundPath) && File.Exists(soundPath) ? soundPath : null;
            }
            
            return null;
        }
        
        public string? ImportSoundToCurrentProfile(string sourceFilePath)
        {
            if (_currentProfile == null || !File.Exists(sourceFilePath) || string.IsNullOrEmpty(_currentProfile.FilePath))
                return null;
            
            try
            {
                var keySoundsDirectory = Path.Combine(_currentProfile.FilePath, "sounds");
                Directory.CreateDirectory(keySoundsDirectory);
                
                var fileName = Path.GetFileName(sourceFilePath);
                if (fileName == null)
                    return null;
                    
                var destFilePath = Path.Combine(keySoundsDirectory, fileName);
                
                var counter = 1;
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                
                while (File.Exists(destFilePath))
                {
                    var newFileName = $"{fileNameWithoutExtension}_{counter}{extension}";
                    destFilePath = Path.Combine(keySoundsDirectory, newFileName);
                    counter++;
                }
                
                File.Copy(sourceFilePath, destFilePath);
                SaveProfile(_currentProfile);
                
                return destFilePath;
            }
            catch
            {
                return null;
            }
        }
        
        public SoundProfile? ImportProfile(string importPath)
        {
            if (!File.Exists(importPath))
                return null;
            
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            
            try
            {
                ZipFile.ExtractToDirectory(importPath, tempDirectory);
                
                var profileJsonPath = Path.Combine(tempDirectory, "index.json");
                if (!File.Exists(profileJsonPath))
                    return null;
                
                var json = File.ReadAllText(profileJsonPath);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new SoundProfileJsonConverter());
                var profile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
                
                if (profile == null)
                    return null;
                
                var profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                if (Directory.Exists(profileDirectory))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    profile.Name = $"{profile.Name}_{timestamp}";
                }
                
                var finalProfileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                Directory.CreateDirectory(finalProfileDirectory);
                profile.FilePath = finalProfileDirectory;
                
                File.Copy(profileJsonPath, Path.Combine(finalProfileDirectory, "index.json"), true);
                
                var sourceKeySoundsDir = Path.Combine(tempDirectory, "sounds");
                if (Directory.Exists(sourceKeySoundsDir))
                {
                    var destKeySoundsDir = Path.Combine(finalProfileDirectory, "sounds");
                    CopyDirectory(sourceKeySoundsDir, destKeySoundsDir);
                }
                
                var reloadedProfile = LoadProfileFromFile(finalProfileDirectory);
                if (reloadedProfile != null)
                {
                    SaveProfile(reloadedProfile);
                    
                    _profiles.Add(reloadedProfile);
                    
                    // 总是切换到新导入的方案
                    SwitchProfile(reloadedProfile);
                    
                    ProfilesChanged?.Invoke(this, EventArgs.Empty);
                    
                    return reloadedProfile;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }
        
        private SoundProfile? LoadProfileFromFile(string profileDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(profileDirectory))
                    return null;
                    
                var profileName = Path.GetFileName(profileDirectory);
                var configFile = Path.Combine(profileDirectory, "index.json");

                if (File.Exists(configFile))
                {
                    var json = File.ReadAllText(configFile);
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new SoundProfileJsonConverter());
                    var profile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
                    if (profile != null)
                    {
                        profile.FilePath = profileDirectory;
                        UpdateKeySoundsFromAssignments(profile);
                        return profile;
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        public bool ExportProfile(SoundProfile profile, string exportPath)
        {
            if (!Directory.Exists(profile.FilePath))
                return false;
            
            try
            {
                ZipFile.CreateFromDirectory(profile.FilePath, exportPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile);
            }
            
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destSubDir);
            }
        }

        public bool RenameProfile(SoundProfile? profile, string newName)
        {
            if (profile == null || string.IsNullOrWhiteSpace(newName) || string.IsNullOrEmpty(profile.FilePath))
                return false;

            if (_profiles.Any(p => p != profile && !string.IsNullOrEmpty(p.Name) && p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                return false;

            string oldPath = profile.FilePath;
            string newPath = Path.Combine(_profilesDirectory, newName);
            
            try
            {
                if (_currentProfile == profile)
                {
                    // 清理当前方案的资源
                    _currentProfile.KeySounds.Clear();
                    CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
                }

                if (Directory.Exists(newPath))
                {
                    Directory.Delete(newPath, true);
                }

                if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                    
                    profile.Name = newName;
                    profile.FilePath = newPath;

                    var profileFile = Path.Combine(profile.FilePath, "index.json");
                    if (File.Exists(profileFile))
                    {
                        var json = File.ReadAllText(profileFile);
                        var settings = new JsonSerializerSettings();
                        settings.Converters.Add(new SoundProfileJsonConverter());
                        var updatedProfile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
                        
                        if (updatedProfile != null)
                        {
                            profile.AssignedSounds = updatedProfile.AssignedSounds;
                        }
                    }

                    UpdateKeySoundsFromAssignments(profile);
                    SaveProfile(profile);
                    
                    if (_currentProfile == profile)
                    {
                        _currentProfile = profile;
                        CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
                    }

                    ProfilesChanged?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                return false;
            }
            catch
            {
                ProfilesChanged?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }
    }
}
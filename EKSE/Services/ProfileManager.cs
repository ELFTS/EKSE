using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
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
        private SoundProfile _currentProfile;
        
        // 定义支持的音频扩展名
        private static readonly string[] SupportedAudioExtensions = { ".wav", ".mp3", ".aac", ".wma", ".flac" };
        
        // 特殊键名映射字典，提取为字段以避免重复创建
        private static readonly Dictionary<string, Key> KeyMap = new Dictionary<string, Key>
        {
            ["Space"] = Key.Space,
            ["Enter"] = Key.Enter,
            ["Backspace"] = Key.Back,
            ["Tab"] = Key.Tab,
            ["Caps"] = Key.CapsLock,
            ["Esc"] = Key.Escape,
            ["Win"] = Key.LWin,
            ["L Shift"] = Key.LeftShift,
            ["R Shift"] = Key.RightShift,
            ["L Ctrl"] = Key.LeftCtrl,
            ["R Ctrl"] = Key.RightCtrl,
            ["L Alt"] = Key.LeftAlt,
            ["R Alt"] = Key.RightAlt,
            ["↑"] = Key.Up,
            ["↓"] = Key.Down,
            ["←"] = Key.Left,
            ["→"] = Key.Right,
            ["[ {"] = Key.OemOpenBrackets,
            ["] }"] = Key.OemCloseBrackets,
            ["; :"] = Key.OemSemicolon,
            ["'"] = Key.OemQuotes,
            [", <"] = Key.OemComma,
            [". >"] = Key.OemPeriod,
            ["/ ?"] = Key.OemQuestion,
            ["\\"] = Key.Oem5,
            ["-"] = Key.OemMinus,
            ["="] = Key.OemPlus,
            ["Del"] = Key.Delete,
            ["Ins"] = Key.Insert,
            ["Home"] = Key.Home,
            ["End"] = Key.End,
            ["PgUp"] = Key.PageUp,
            ["PgDn"] = Key.PageDown,
            ["Pause"] = Key.Pause,
            ["SrcLk"] = Key.Scroll,
            ["Fn"] = Key.None
        };
        
        public ProfileManager()
        {
            _profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
            _profiles = new List<SoundProfile>();
            
            // 确保Profiles目录存在
            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
            }
            
            // 加载现有方案
            LoadProfiles();
            
            // 如果没有方案，则创建默认方案
            if (!_profiles.Any())
            {
                CreateDefaultProfile();
            }
            
            // 设置当前方案为第一个方案
            _currentProfile = _profiles.FirstOrDefault();
        }
        
        /// <summary>
        /// 获取所有声音方案
        /// </summary>
        public IReadOnlyList<SoundProfile> Profiles => _profiles.AsReadOnly();
        
        /// <summary>
        /// 获取当前声音方案
        /// </summary>
        public SoundProfile CurrentProfile => _currentProfile;
        
        /// <summary>
        /// 加载所有声音方案
        /// </summary>
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
                            // 加载现有配置文件
                            var json = File.ReadAllText(configFile);
                            var options = new JsonSerializerOptions();
                            options.Converters.Add(new SoundProfileJsonConverter());
                            profile = JsonSerializer.Deserialize<SoundProfile>(json, options);
                            if (profile != null)
                            {
                                profile.FilePath = directory;
                                // 将分配的声音转换为按键声音映射
                                ConvertAssignedSoundsToKeySounds(profile);
                            }
                            else
                            {
                                // 如果配置文件损坏，创建新的配置
                                profile = new SoundProfile(profileName)
                                {
                                    FilePath = directory
                                };
                            }
                        }
                        else
                        {
                            // 创建新的配置
                            profile = new SoundProfile(profileName)
                            {
                                FilePath = directory
                            };
                        }
                        
                        // 加载按键音效映射
                        LoadKeySounds(profile);
                        
                        _profiles.Add(profile);
                    }
                    catch (Exception ex)
                    {
                        // 忽略单个方案加载错误
                        System.Diagnostics.Debug.WriteLine($"加载方案失败: {directory}, 错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载声音方案时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 将分配的声音转换为按键声音映射
        /// </summary>
        /// <param name="profile">声音方案</param>
        private void ConvertAssignedSoundsToKeySounds(SoundProfile profile)
        {
            profile.KeySounds.Clear();
            if (profile.AssignedSounds?.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"开始转换 {profile.AssignedSounds.Count} 个分配的声音");
                
                foreach (var assignment in profile.AssignedSounds)
                {
                    // 使用增强的按键解析功能
                    var key = ParseKeyName(assignment.Key);
                    if (key.HasValue)
                    {
                        // 构建完整的音效文件路径
                        var soundPath = Path.Combine(profile.FilePath, "sounds", assignment.Sound);
                        profile.KeySounds[key.Value] = soundPath;
                        System.Diagnostics.Debug.WriteLine($"映射按键 {key.Value} 到文件 {soundPath}");
                        
                        // 特别关注数字键
                        if (key.Value >= Key.D0 && key.Value <= Key.D9)
                        {
                            System.Diagnostics.Debug.WriteLine($"数字键映射: {assignment.Key} -> {key.Value} -> {soundPath}");
                        }
                    }
                    else
                    {
                        // 如果无法解析按键名称，记录警告信息
                        System.Diagnostics.Debug.WriteLine($"无法解析按键名称: {assignment.Key}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 解析按键名称，支持数字键和其他特殊键名
        /// </summary>
        /// <param name="keyName">按键名称</param>
        /// <returns>解析后的Key值，如果无法解析则返回null</returns>
        private Key? ParseKeyName(string keyName)
        {
            System.Diagnostics.Debug.WriteLine($"尝试解析按键名称: '{keyName}'");
            
            // 处理单独的数字字符 '0'-'9'
            if (keyName.Length == 1 && char.IsDigit(keyName[0]))
            {
                return keyName[0] switch
                {
                    '0' => Key.D0,
                    '1' => Key.D1,
                    '2' => Key.D2,
                    '3' => Key.D3,
                    '4' => Key.D4,
                    '5' => Key.D5,
                    '6' => Key.D6,
                    '7' => Key.D7,
                    '8' => Key.D8,
                    '9' => Key.D9,
                    _ => null
                };
            }
            
            // 处理数字键 (D1, D2, D3 等)
            if (keyName.Length == 2 && keyName.StartsWith("D") && char.IsDigit(keyName[1]))
            {
                return keyName[1] switch
                {
                    '0' => Key.D0,
                    '1' => Key.D1,
                    '2' => Key.D2,
                    '3' => Key.D3,
                    '4' => Key.D4,
                    '5' => Key.D5,
                    '6' => Key.D6,
                    '7' => Key.D7,
                    '8' => Key.D8,
                    '9' => Key.D9,
                    _ => null
                };
            }
            
            // 首先尝试直接解析
            if (Enum.TryParse<Key>(keyName, true, out var key))
            {
                System.Diagnostics.Debug.WriteLine($"直接解析成功: {keyName} -> {key}");
                return key;
            }
            
            // 处理特殊键名映射
            if (KeyMap.TryGetValue(keyName, out var mappedKey))
            {
                System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> {mappedKey}");
                return mappedKey;
            }
            
            // 如果以上都无法匹配，记录并返回null
            System.Diagnostics.Debug.WriteLine($"无法解析按键名称: '{keyName}'");
            return null;
        }
        
        /// <summary>
        /// 加载方案中的按键音效映射
        /// </summary>
        /// <param name="profile">声音方案</param>
        private void LoadKeySounds(SoundProfile profile)
        {
            try
            {
                var keySoundsDirectory = Path.Combine(profile.FilePath, "sounds");
                if (!Directory.Exists(keySoundsDirectory)) 
                {
                    System.Diagnostics.Debug.WriteLine($"音效目录不存在: {keySoundsDirectory}");
                    return;
                }

                var soundFiles = Directory.GetFiles(keySoundsDirectory, "*.*", SearchOption.AllDirectories)
                    .Where(file => SupportedAudioExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

                System.Diagnostics.Debug.WriteLine($"在目录 {keySoundsDirectory} 中找到 {soundFiles.Count()} 个音效文件");

                foreach (var soundFile in soundFiles)
                {
                    var keyName = Path.GetFileNameWithoutExtension(soundFile);
                    System.Diagnostics.Debug.WriteLine($"处理音效文件: {keyName} ({soundFile})");

                    // 使用增强的按键解析功能
                    var key = ParseKeyName(keyName);
                    if (key.HasValue)
                    {
                        profile.KeySounds[key.Value] = soundFile;
                        System.Diagnostics.Debug.WriteLine($"映射按键 {key.Value} 到文件 {soundFile}");

                        // 特别关注数字键
                        if (key.Value >= Key.D0 && key.Value <= Key.D9)
                        {
                            System.Diagnostics.Debug.WriteLine($"数字键映射: {keyName} -> {key.Value} -> {soundFile}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"无法解析按键名称: {keyName}");
                    }
                }

                // 不再加载默认音效
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载按键音效映射时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建默认声音方案
        /// </summary>
        private void CreateDefaultProfile()
        {
            var defaultProfile = new SoundProfile("默认方案");
            var profileDirectory = Path.Combine(_profilesDirectory, defaultProfile.Name);
            
            // 确保方案目录存在
            Directory.CreateDirectory(profileDirectory);
            defaultProfile.FilePath = profileDirectory;
            
            // 复制当前方案的按键映射
            CopyCurrentProfileMappings(defaultProfile);
            
            _profiles.Add(defaultProfile);
            SaveProfile(defaultProfile);
        }
        
        /// <summary>
        /// 创建声音方案
        /// </summary>
        /// <param name="name">方案名称</param>
        /// <returns>创建的方案</returns>
        public SoundProfile CreateProfile(string name)
        {
            var profile = new SoundProfile(name);
            var profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
            
            // 确保方案目录存在
            Directory.CreateDirectory(profileDirectory);
            profile.FilePath = profileDirectory;
            
            // 复制当前方案的按键映射
            CopyCurrentProfileMappings(profile);
            
            _profiles.Add(profile);
            SaveProfile(profile);
            return profile;
        }
        
        /// <summary>
        /// 复制当前方案的按键映射
        /// </summary>
        /// <param name="targetProfile">目标方案</param>
        private void CopyCurrentProfileMappings(SoundProfile targetProfile)
        {
            if (_currentProfile == null) return;
            
            foreach (var kvp in _currentProfile.KeySounds)
            {
                targetProfile.KeySounds[kvp.Key] = kvp.Value;
            }
        }
        
        /// <summary>
        /// 删除声音方案
        /// </summary>
        /// <param name="profile">要删除的方案</param>
        public void DeleteProfile(SoundProfile profile)
        {
            // 至少需要保留一个方案
            if (_profiles.Count <= 1)
            {
                System.Diagnostics.Debug.WriteLine("至少需要保留一个声音方案");
                return;
            }
            
            if (!_profiles.Remove(profile))
                return;
            
            // 尝试删除方案文件夹
            DeleteProfileDirectory(profile.FilePath);
            
            // 如果删除的是当前方案，则设置新的当前方案
            if (_currentProfile == profile)
            {
                _currentProfile = _profiles.FirstOrDefault();
            }
        }
        
        /// <summary>
        /// 删除方案目录
        /// </summary>
        /// <param name="profilePath">方案目录路径</param>
        private void DeleteProfileDirectory(string profilePath)
        {
            try
            {
                // 尝试删除方案文件夹（最多重试3次）
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        if (Directory.Exists(profilePath))
                        {
                            Directory.Delete(profilePath, true);
                        }
                        break; // 成功删除则跳出循环
                    }
                    catch (IOException)
                    {
                        // 如果是最后一次尝试，则重新抛出异常
                        if (i == 2) throw;
                        
                        // 等待一段时间后重试
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除方案文件夹失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 保存声音方案
        /// </summary>
        /// <param name="profile">要保存的方案</param>
        public void SaveProfile(SoundProfile profile)
        {
            try
            {
                var profileFile = Path.Combine(profile.FilePath, "index.json");
                
                // 将按键声音映射转换为分配的声音列表
                ConvertKeySoundsToAssignedSounds(profile);
                
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                options.Converters.Add(new SoundProfileJsonConverter());
                
                var json = JsonSerializer.Serialize(profile, options);
                File.WriteAllText(profileFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存方案失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 将按键声音映射转换为分配的声音列表
        /// </summary>
        /// <param name="profile">声音方案</param>
        private void ConvertKeySoundsToAssignedSounds(SoundProfile profile)
        {
            // 只有当AssignedSounds为空时才从KeySounds生成，避免覆盖已有的数据
            if (profile.AssignedSounds?.Count > 0) return;
            
            profile.AssignedSounds = new List<SoundAssignment>();
            foreach (var kvp in profile.KeySounds)
            {
                // 提取音效文件名
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
        
        /// <summary>
        /// 设置当前声音方案
        /// </summary>
        /// <param name="profile">要设置为当前的方案</param>
        public void SetCurrentProfile(SoundProfile profile)
        {
            if (_profiles.Contains(profile))
            {
                _currentProfile = profile;
            }
        }
        
        /// <summary>
        /// 设置按键的音效文件路径
        /// </summary>
        /// <param name="key">按键</param>
        /// <param name="soundPath">音效文件路径</param>
        public void SetKeySound(Key key, string soundPath)
        {
            if (_currentProfile != null && File.Exists(soundPath))
            {
                try
                {
                    // 直接引用原始文件路径，而不是复制文件
                    _currentProfile.KeySounds[key] = soundPath;
                    SaveProfile(_currentProfile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"设置按键音效失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 获取按键的音效文件路径
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>音效文件路径</returns>
        public string GetKeySound(Key key)
        {
            if (_currentProfile != null && _currentProfile.KeySounds.ContainsKey(key))
            {
                var soundPath = _currentProfile.KeySounds[key];
                return File.Exists(soundPath) ? soundPath : null;
            }
            
            return null;
        }
        
        /// <summary>
        /// 导入音效文件到当前方案
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <returns>导入后的文件路径</returns>
        public string ImportSoundToCurrentProfile(string sourceFilePath)
        {
            if (_currentProfile == null || !File.Exists(sourceFilePath))
                return null;
            
            try
            {
                var keySoundsDirectory = Path.Combine(_currentProfile.FilePath, "sounds");
                if (!Directory.Exists(keySoundsDirectory))
                {
                    Directory.CreateDirectory(keySoundsDirectory);
                }
                
                var fileName = Path.GetFileName(sourceFilePath);
                var destFilePath = Path.Combine(keySoundsDirectory, fileName);
                
                // 如果目标文件已存在，先删除它
                if (File.Exists(destFilePath))
                {
                    File.Delete(destFilePath);
                }
                
                // 复制文件
                File.Copy(sourceFilePath, destFilePath, true);
                
                // 尝试从文件名解析按键并添加到KeySounds映射中
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                var key = ParseKeyName(fileNameWithoutExt);
                if (key.HasValue)
                {
                    _currentProfile.KeySounds[key.Value] = destFilePath;
                    SaveProfile(_currentProfile);
                }
                
                return destFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入音效文件失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 导出声音方案为ZIP文件
        /// </summary>
        /// <param name="profile">要导出的声音方案</param>
        /// <param name="exportPath">导出路径</param>
        /// <returns>是否导出成功</returns>
        public bool ExportProfile(SoundProfile profile, string exportPath)
        {
            // 创建临时目录用于打包
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            
            try
            {
                // 复制index.json文件
                var profileJsonPath = Path.Combine(profile.FilePath, "index.json");
                if (File.Exists(profileJsonPath))
                {
                    File.Copy(profileJsonPath, Path.Combine(tempDirectory, "index.json"));
                }
                
                // 复制sounds文件夹
                var sourceKeySoundsDir = Path.Combine(profile.FilePath, "sounds");
                var destKeySoundsDir = Path.Combine(tempDirectory, "sounds");
                if (Directory.Exists(sourceKeySoundsDir))
                {
                    CopyDirectory(sourceKeySoundsDir, destKeySoundsDir);
                }
                
                
                // 创建ZIP文件
                ZipFile.CreateFromDirectory(tempDirectory, exportPath);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出声音方案失败: {ex.Message}");
                return false;
            }
            finally
            {
                // 清理临时目录
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
        }
        
        /// <summary>
        /// 从ZIP文件导入声音方案
        /// </summary>
        /// <param name="importPath">ZIP文件路径</param>
        /// <returns>导入的声音方案，如果失败则返回null</returns>
        public SoundProfile ImportProfile(string importPath)
        {
            if (!File.Exists(importPath))
            {
                return null;
            }
            
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            
            try
            {
                ZipFile.ExtractToDirectory(importPath, tempDirectory);
                
                var profileJsonPath = Path.Combine(tempDirectory, "index.json");
                if (!File.Exists(profileJsonPath))
                {
                    return null;
                }
                
                var json = File.ReadAllText(profileJsonPath);
                var options = new JsonSerializerOptions();
                options.Converters.Add(new SoundProfileJsonConverter());
                var profile = JsonSerializer.Deserialize<SoundProfile>(json, options);
                
                // 处理方案名称冲突
                var profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                if (Directory.Exists(profileDirectory))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    profile.Name = $"{profile.Name}_{timestamp}";
                    profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                }
                
                // 创建方案目录并复制文件
                Directory.CreateDirectory(profileDirectory);
                profile.FilePath = profileDirectory;
                
                var sourceKeySoundsDir = Path.Combine(tempDirectory, "sounds");
                var destKeySoundsDir = Path.Combine(profileDirectory, "sounds");
                if (Directory.Exists(sourceKeySoundsDir))
                {
                    CopyDirectory(sourceKeySoundsDir, destKeySoundsDir);
                }
                
                
                // 重建KeySounds映射
                RebuildKeySoundsMapping(profile, destKeySoundsDir);
                
                SaveProfile(profile);
                _profiles.Add(profile);
                
                return profile;
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
        
        /// <summary>
        /// 根据AssignedSounds或文件列表重建KeySounds映射
        /// </summary>
        /// <param name="profile">声音方案</param>
        /// <param name="destKeySoundsDir">目标音效目录</param>
        private void RebuildKeySoundsMapping(SoundProfile profile, string destKeySoundsDir)
        {
            if (profile.AssignedSounds?.Count > 0)
            {
                profile.KeySounds.Clear();
                foreach (var assignment in profile.AssignedSounds)
                {
                    var key = ParseKeyName(assignment.Key);
                    if (key.HasValue)
                    {
                        var soundPath = Path.Combine(profile.FilePath, "sounds", assignment.Sound);
                        profile.KeySounds[key.Value] = soundPath;
                    }
                }
            }
            else if (Directory.Exists(destKeySoundsDir))
            {
                var soundFiles = Directory.GetFiles(destKeySoundsDir, "*.*", SearchOption.AllDirectories)
                    .Where(file => SupportedAudioExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));
                
                foreach (var soundFile in soundFiles)
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(soundFile);
                    var key = ParseKeyName(fileNameWithoutExt);
                    if (key.HasValue)
                    {
                        profile.KeySounds[key.Value] = soundFile;
                    }
                }
            }
        }
        
        /// <summary>
        /// 复制目录及其内容
        /// </summary>
        /// <param name="sourceDir">源目录</param>
        /// <param name="destDir">目标目录</param>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
            }
            
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(directory, Path.Combine(destDir, Path.GetFileName(directory)));
            }
        }
    }
}
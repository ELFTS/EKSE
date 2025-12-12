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
        private SoundProfile _currentProfile;
        
        // 添加事件，当方案列表发生变化时触发
        public event EventHandler ProfilesChanged;
        
        // 添加事件，当当前方案改变时触发
        public event EventHandler CurrentProfileChanged;
        
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
                            var settings = new JsonSerializerSettings();
                            settings.Converters.Add(new SoundProfileJsonConverter());
                            profile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
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
                        
                        // 检查文件是否存在，只有存在时才添加映射
                        if (File.Exists(soundPath))
                        {
                            profile.KeySounds[key.Value] = soundPath;
                            System.Diagnostics.Debug.WriteLine($"映射按键 {key.Value} 到文件 {soundPath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"音效文件不存在，跳过映射: {soundPath}");
                        }
                        
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
            // 只使用JSON配置文件中的AssignedSounds数据
            if (profile.AssignedSounds?.Count > 0)
            {
                profile.KeySounds.Clear();
                foreach (var assignment in profile.AssignedSounds)
                {
                    var key = ParseKeyName(assignment.Key);
                    if (key.HasValue)
                    {
                        var soundPath = Path.Combine(profile.FilePath, "sounds", assignment.Sound);
                        if (File.Exists(soundPath))
                        {
                            profile.KeySounds[key.Value] = soundPath;
                            System.Diagnostics.Debug.WriteLine($"从JSON配置映射按键 {key.Value} 到文件 {soundPath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"JSON配置中指定的文件不存在: {soundPath}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"无法解析JSON配置中的按键名称: {assignment.Key}");
                    }
                }
            }
            // 如果没有AssignedSounds数据，则不进行任何操作
            else
            {
                System.Diagnostics.Debug.WriteLine("没有找到JSON配置中的AssignedSounds数据");
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
            
            // 触发方案列表变化事件
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
            
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
                // 触发当前方案改变事件
                CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
            }
            
            // 触发方案列表变化事件
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// 删除方案目录
        /// </summary>
        /// <param name="profilePath">方案目录路径</param>
        private void DeleteProfileDirectory(string profilePath)
        {
            try
            {
                if (Directory.Exists(profilePath))
                {
                    Directory.Delete(profilePath, true);
                }
            }
            catch
            {
                // 静默忽略删除失败，不进行重试或强制删除
            }
        }
        
        /// <summary>
        /// 递归删除目录内容，处理可能被占用的文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        private void DeleteDirectoryContents(string directoryPath)
        {
            try
            {
                // 删除所有文件
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    try
                    {
                        // 检查文件是否只读，如果是则清除只读属性
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                        }
                        
                        // 尝试强制删除文件
                        ForceDeleteFile(file);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"删除文件失败 {file}: {ex.Message}");
                        // 继续尝试删除其他文件，不抛出异常
                    }
                }
                
                // 递归删除子目录
                var subdirectories = Directory.GetDirectories(directoryPath);
                foreach (var subdirectory in subdirectories)
                {
                    DeleteDirectoryContents(subdirectory);
                    try
                    {
                        Directory.Delete(subdirectory, true);
                        System.Diagnostics.Debug.WriteLine($"成功删除子目录: {subdirectory}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"删除子目录失败 {subdirectory}: {ex.Message}");
                        // 继续处理其他子目录，不抛出异常
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理目录内容时出错 {directoryPath}: {ex.Message}");
                // 不抛出异常，允许调用者继续尝试删除整个目录
            }
        }
        
        /// <summary>
        /// 强制删除文件，尝试多种方法解除文件占用
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private void ForceDeleteFile(string filePath)
        {
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        System.Diagnostics.Debug.WriteLine($"成功删除文件: {filePath}");
                    }
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    System.Diagnostics.Debug.WriteLine($"第{i + 1}次尝试删除文件失败（未授权访问）: {filePath}");
                    
                    // 尝试清除只读属性
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                        }
                    }
                    catch
                    {
                        // 忽略设置只读属性时的错误
                    }
                    
                    if (i == maxRetries - 1) throw; // 最后一次尝试，重新抛出异常
                    
                    // 等待后重试
                    System.Threading.Thread.Sleep(100);
                }
                catch (IOException)
                {
                    System.Diagnostics.Debug.WriteLine($"第{i + 1}次尝试删除文件失败（IO异常）: {filePath}");
                    if (i == maxRetries - 1) throw; // 最后一次尝试，重新抛出异常
                    
                    // 等待后重试
                    System.Threading.Thread.Sleep(100);
                }
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
                
                var settings = new JsonSerializerSettings 
                { 
                    Formatting = Formatting.Indented
                };
                settings.Converters.Add(new SoundProfileJsonConverter());
                
                var json = JsonConvert.SerializeObject(profile, settings);
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
            // 总是根据当前的KeySounds更新AssignedSounds，确保数据一致性
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
                // 触发当前方案改变事件
                CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
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
        /// 将音效文件导入到当前方案
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
                Directory.CreateDirectory(keySoundsDirectory);
                
                var fileName = Path.GetFileName(sourceFilePath);
                var destFilePath = Path.Combine(keySoundsDirectory, fileName);
                
                // 如果目标文件已存在，生成唯一文件名
                var counter = 1;
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(fileName);
                
                while (File.Exists(destFilePath))
                {
                    var newFileName = $"{fileNameWithoutExtension}_{counter}{extension}";
                    destFilePath = Path.Combine(keySoundsDirectory, newFileName);
                    counter++;
                }
                
                // 复制文件
                File.Copy(sourceFilePath, destFilePath);
                
                // 直接保存配置文件
                SaveProfile(_currentProfile);
                
                return destFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入音效文件失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 从目录加载声音方案
        /// </summary>
        /// <param name="profileDirectory">方案目录</param>
        /// <returns>声音方案</returns>
        private SoundProfile LoadProfileFromFile(string profileDirectory)
        {
            try
            {
                var profileName = Path.GetFileName(profileDirectory);
                var configFile = Path.Combine(profileDirectory, "index.json");

                if (File.Exists(configFile))
                {
                    // 加载现有配置文件
                    var json = File.ReadAllText(configFile);
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new SoundProfileJsonConverter());
                    var profile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
                    if (profile != null)
                    {
                        profile.FilePath = profileDirectory;
                        return profile;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载方案失败: {profileDirectory}, 错误: {ex.Message}");
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
                // 解压ZIP文件
                ZipFile.ExtractToDirectory(importPath, tempDirectory);
                
                // 读取配置文件
                var profileJsonPath = Path.Combine(tempDirectory, "index.json");
                if (!File.Exists(profileJsonPath))
                {
                    return null;
                }
                
                var json = File.ReadAllText(profileJsonPath);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new SoundProfileJsonConverter());
                var profile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
                
                if (profile == null)
                {
                    return null;
                }
                
                // 处理方案名称冲突
                var originalName = profile.Name;
                var profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                if (Directory.Exists(profileDirectory))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    profile.Name = $"{profile.Name}_{timestamp}";
                }
                
                // 创建方案目录并复制文件
                var finalProfileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                Directory.CreateDirectory(finalProfileDirectory);
                profile.FilePath = finalProfileDirectory;
                
                // 复制index.json文件
                File.Copy(profileJsonPath, Path.Combine(finalProfileDirectory, "index.json"), true);
                
                // 复制sounds目录
                var sourceKeySoundsDir = Path.Combine(tempDirectory, "sounds");
                if (Directory.Exists(sourceKeySoundsDir))
                {
                    var destKeySoundsDir = Path.Combine(finalProfileDirectory, "sounds");
                    CopyDirectory(sourceKeySoundsDir, destKeySoundsDir);
                }
                
                // 重新加载配置以确保数据一致性
                var reloadedProfile = LoadProfileFromFile(finalProfileDirectory);
                if (reloadedProfile != null)
                {
                    _profiles.Add(reloadedProfile);
                    
                    // 触发方案列表变化事件
                    ProfilesChanged?.Invoke(this, EventArgs.Empty);
                    
                    return reloadedProfile;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入声音方案失败: {ex.Message}");
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
        /// 根据AssignedSounds重建KeySounds映射
        /// </summary>
        /// <param name="profile">声音方案</param>
        /// <param name="destKeySoundsDir">目标音效目录</param>
        private void RebuildKeySoundsMapping(SoundProfile profile, string destKeySoundsDir)
        {
            // 只使用JSON配置文件中的AssignedSounds数据
            if (profile.AssignedSounds?.Count > 0)
            {
                profile.KeySounds.Clear();
                foreach (var assignment in profile.AssignedSounds)
                {
                    var key = ParseKeyName(assignment.Key);
                    if (key.HasValue)
                    {
                        var soundPath = Path.Combine(profile.FilePath, "sounds", assignment.Sound);
                        if (File.Exists(soundPath))
                        {
                            profile.KeySounds[key.Value] = soundPath;
                            System.Diagnostics.Debug.WriteLine($"从JSON配置映射按键 {key.Value} 到文件 {soundPath}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"JSON配置中指定的文件不存在: {soundPath}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"无法解析JSON配置中的按键名称: {assignment.Key}");
                    }
                }
            }
            // 如果没有AssignedSounds数据，则不进行任何操作
            else
            {
                System.Diagnostics.Debug.WriteLine("没有找到JSON配置中的AssignedSounds数据");
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
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile);
            }
            
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destSubDir);
            }
        }

        /// <summary>
        /// 重命名声音方案
        /// </summary>
        /// <param name="profile">要重命名的声音方案</param>
        /// <param name="newName">新名称</param>
        /// <returns>是否重命名成功</returns>
        public bool RenameProfile(SoundProfile profile, string newName)
        {
            if (profile == null || string.IsNullOrWhiteSpace(newName))
                return false;

            // 检查新名称是否与现有方案名称冲突
            if (_profiles.Any(p => p != profile && p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                return false;

            string oldPath = profile.FilePath;
            string newPath = Path.Combine(_profilesDirectory, newName);
            
            try
            {
                // 通知其他组件当前方案即将改变，以便释放相关资源
                if (_currentProfile == profile)
                {
                    CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
                }

                // 如果新路径已存在，则先删除
                if (Directory.Exists(newPath))
                {
                    Directory.Delete(newPath, true);
                }

                // 直接重命名目录
                Directory.Move(oldPath, newPath);
                
                // 保存旧名称用于后续处理
                string oldName = profile.Name;
                
                // 更新方案属性
                profile.Name = newName;
                profile.FilePath = newPath;

                // 重新加载方案以确保AssignedSounds数据是最新的
                var profileFile = Path.Combine(profile.FilePath, "index.json");
                if (File.Exists(profileFile))
                {
                    var json = File.ReadAllText(profileFile);
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new SoundProfileJsonConverter());
                    var updatedProfile = JsonConvert.DeserializeObject<SoundProfile>(json, settings);
                    
                    // 更新当前方案的AssignedSounds数据
                    if (updatedProfile != null)
                    {
                        profile.AssignedSounds = updatedProfile.AssignedSounds;
                    }
                }

                // 重新构建按键音效映射
                LoadKeySounds(profile);

                // 保存更新后的方案
                SaveProfile(profile);
                
                // 如果重命名的是当前方案，则更新_currentProfile引用
                if (_currentProfile == profile)
                {
                    _currentProfile = profile;
                    // 触发当前方案改变事件
                    CurrentProfileChanged?.Invoke(this, EventArgs.Empty);
                }

                // 触发方案列表变化事件，确保UI更新
                ProfilesChanged?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重命名声音方案失败: {ex.Message}");
                
                // 即使发生异常也要触发事件以确保UI状态一致
                ProfilesChanged?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }
    }
}
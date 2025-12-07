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
            if (profile.AssignedSounds != null)
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
                switch (keyName[0])
                {
                    case '0': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D0"); return Key.D0;
                    case '1': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D1"); return Key.D1;
                    case '2': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D2"); return Key.D2;
                    case '3': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D3"); return Key.D3;
                    case '4': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D4"); return Key.D4;
                    case '5': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D5"); return Key.D5;
                    case '6': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D6"); return Key.D6;
                    case '7': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D7"); return Key.D7;
                    case '8': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D8"); return Key.D8;
                    case '9': System.Diagnostics.Debug.WriteLine($"解析直接数字键: {keyName} -> D9"); return Key.D9;
                }
            }
            
            // 处理数字键 (D1, D2, D3 等)
            if (keyName.Length == 2 && keyName.StartsWith("D") && char.IsDigit(keyName[1]))
            {
                var digit = keyName[1];
                switch (digit)
                {
                    case '0': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D0"); return Key.D0;
                    case '1': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D1"); return Key.D1;
                    case '2': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D2"); return Key.D2;
                    case '3': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D3"); return Key.D3;
                    case '4': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D4"); return Key.D4;
                    case '5': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D5"); return Key.D5;
                    case '6': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D6"); return Key.D6;
                    case '7': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D7"); return Key.D7;
                    case '8': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D8"); return Key.D8;
                    case '9': System.Diagnostics.Debug.WriteLine($"解析D格式数字键: {keyName} -> D9"); return Key.D9;
                }
            }
            
            // 首先尝试直接解析
            if (Enum.TryParse<Key>(keyName, true, out var key))
            {
                System.Diagnostics.Debug.WriteLine($"直接解析成功: {keyName} -> {key}");
                return key;
            }
            
            // 处理特殊键名映射
            switch (keyName)
            {
                case "Space": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Space"); return Key.Space;
                case "Enter": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Enter"); return Key.Enter;
                case "Backspace": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Back"); return Key.Back;
                case "Tab": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Tab"); return Key.Tab;
                case "Caps": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> CapsLock"); return Key.CapsLock;
                case "Esc": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Escape"); return Key.Escape;
                case "Win": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> LWin"); return Key.LWin; // 或 Key.RWin
                case "L Shift": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> LeftShift"); return Key.LeftShift;
                case "R Shift": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> RightShift"); return Key.RightShift;
                case "L Ctrl": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> LeftCtrl"); return Key.LeftCtrl;
                case "R Ctrl": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> RightCtrl"); return Key.RightCtrl;
                case "L Alt": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> LeftAlt"); return Key.LeftAlt;
                case "R Alt": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> RightAlt"); return Key.RightAlt;
                case "↑": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Up"); return Key.Up;
                case "↓": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Down"); return Key.Down;
                case "←": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Left"); return Key.Left;
                case "→": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Right"); return Key.Right;
                case "[ {": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemOpenBrackets"); return Key.OemOpenBrackets;
                case "] }": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemCloseBrackets"); return Key.OemCloseBrackets;
                case "; :": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemSemicolon"); return Key.OemSemicolon;
                case "'": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemQuotes"); return Key.OemQuotes;
                case ", <": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemComma"); return Key.OemComma;
                case ". >": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemPeriod"); return Key.OemPeriod;
                case "/ ?": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemQuestion"); return Key.OemQuestion;
                case "\\": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Oem5"); return Key.Oem5;
                case "-": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemMinus"); return Key.OemMinus;
                case "=": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> OemPlus"); return Key.OemPlus; // 注意：在键盘上+和=是同一个键
                case "Del": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Delete"); return Key.Delete;
                case "Ins": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Insert"); return Key.Insert;
                case "Home": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Home"); return Key.Home;
                case "End": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> End"); return Key.End;
                case "PgUp": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> PageUp"); return Key.PageUp;
                case "PgDn": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> PageDown"); return Key.PageDown;
                case "Pause": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Pause"); return Key.Pause;
                case "SrcLk": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> Scroll"); return Key.Scroll;
                case "Fn": System.Diagnostics.Debug.WriteLine($"解析特殊键: {keyName} -> None"); return Key.None; // Fn键通常不被系统识别
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
                if (Directory.Exists(keySoundsDirectory))
                {
                    var soundFiles = Directory.GetFiles(keySoundsDirectory, "*.*", SearchOption.AllDirectories)
                        .Where(file => new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" }
                            .Contains(Path.GetExtension(file).ToLowerInvariant()));
                    
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
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"音效目录不存在: {keySoundsDirectory}");
                }
                
                // 加载默认音效
                var defaultSoundFile = Path.Combine(profile.FilePath, "DefaultSound.wav");
                if (File.Exists(defaultSoundFile))
                {
                    profile.DefaultSound = defaultSoundFile;
                    System.Diagnostics.Debug.WriteLine($"加载默认音效: {defaultSoundFile}");
                }
                else
                {
                    // 尝试其他音频格式
                    var supportedExtensions = new[] { ".mp3", ".aac", ".wma", ".flac" };
                    foreach (var extension in supportedExtensions)
                    {
                        var file = Path.Combine(profile.FilePath, "DefaultSound" + extension);
                        if (File.Exists(file))
                        {
                            profile.DefaultSound = file;
                            System.Diagnostics.Debug.WriteLine($"加载默认音效: {file}");
                            break;
                        }
                    }
                }
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
            if (!Directory.Exists(profileDirectory))
            {
                Directory.CreateDirectory(profileDirectory);
            }
            
            defaultProfile.FilePath = profileDirectory;
            
            // 复制当前方案的按键映射
            if (_currentProfile != null)
            {
                foreach (var kvp in _currentProfile.KeySounds)
                {
                    defaultProfile.KeySounds[kvp.Key] = kvp.Value;
                }
                defaultProfile.DefaultSound = _currentProfile.DefaultSound;
            }
            
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
            if (!Directory.Exists(profileDirectory))
            {
                Directory.CreateDirectory(profileDirectory);
            }
            
            profile.FilePath = profileDirectory;
            
            // 复制当前方案的按键映射
            if (_currentProfile != null)
            {
                foreach (var kvp in _currentProfile.KeySounds)
                {
                    profile.KeySounds[kvp.Key] = kvp.Value;
                }
                profile.DefaultSound = _currentProfile.DefaultSound;
            }
            
            _profiles.Add(profile);
            SaveProfile(profile);
            return profile;
        }
        
        /// <summary>
        /// 删除声音方案
        /// </summary>
        /// <param name="profile">要删除的方案</param>
        public void DeleteProfile(SoundProfile profile)
        {
            if (_profiles.Count <= 1)
            {
                System.Diagnostics.Debug.WriteLine("至少需要保留一个声音方案");
                return;
            }
            
            if (_profiles.Remove(profile))
            {
                try
                {
                    // 尝试删除方案文件夹（最多重试3次）
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            if (Directory.Exists(profile.FilePath))
                            {
                                Directory.Delete(profile.FilePath, true);
                            }
                            break; // 成功删除则跳出循环
                        }
                        catch (IOException)
                        {
                            // 如果是最后一次尝试，则重新抛出异常
                            if (i == 2)
                                throw;
                            
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
            
            // 如果删除的是当前方案，则设置新的当前方案
            if (_currentProfile == profile)
            {
                _currentProfile = _profiles.FirstOrDefault();
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
            if (profile.AssignedSounds == null || profile.AssignedSounds.Count == 0)
            {
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
            // 如果AssignedSounds已经有数据，则保持不变
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
        public void SetKeySound(System.Windows.Input.Key key, string soundPath)
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
        public string GetKeySound(System.Windows.Input.Key key)
        {
            if (_currentProfile != null && _currentProfile.KeySounds.ContainsKey(key))
            {
                var soundPath = _currentProfile.KeySounds[key];
                if (File.Exists(soundPath))
                {
                    return soundPath;
                }
            }
            
            return _currentProfile?.DefaultSound;
        }
        
        /// <summary>
        /// 设置默认音效
        /// </summary>
        /// <param name="soundPath">音效文件路径</param>
        public void SetDefaultSound(string soundPath)
        {
            if (_currentProfile != null && File.Exists(soundPath))
            {
                try
                {
                    var extension = Path.GetExtension(soundPath);
                    var destFileName = "DefaultSound" + extension;
                    var destFilePath = Path.Combine(_currentProfile.FilePath, destFileName);
                    
                    // 如果目标文件已存在，先删除它
                    if (File.Exists(destFilePath))
                    {
                        try 
                        {
                            File.Delete(destFilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"删除已存在的默认音效文件失败: {ex.Message}");
                        }
                    }
                    
                    // 复制文件
                    File.Copy(soundPath, destFilePath, true);
                    
                    // 更新默认音效路径
                    _currentProfile.DefaultSound = destFilePath;
                    SaveProfile(_currentProfile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"设置默认音效失败: {ex.Message}");
                }
            }
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
                    try 
                    {
                        File.Delete(destFilePath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"删除已存在的音效文件失败: {ex.Message}");
                    }
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
            try
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
                    
                    // 复制默认音效文件
                    if (!string.IsNullOrEmpty(profile.DefaultSound) && File.Exists(profile.DefaultSound))
                    {
                        var defaultSoundFileName = Path.GetFileName(profile.DefaultSound);
                        File.Copy(profile.DefaultSound, Path.Combine(tempDirectory, defaultSoundFileName));
                    }
                    
                    // 创建ZIP文件
                    ZipFile.CreateFromDirectory(tempDirectory, exportPath);
                    
                    return true;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出声音方案失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从ZIP文件导入声音方案
        /// </summary>
        /// <param name="importPath">ZIP文件路径</param>
        /// <returns>导入的声音方案，如果失败则返回null</returns>
        public SoundProfile ImportProfile(string importPath)
        {
            try
            {
                // 检查ZIP文件是否存在
                if (!File.Exists(importPath))
                {
                    System.Diagnostics.Debug.WriteLine("指定的ZIP文件不存在");
                    return null;
                }
                
                // 创建临时目录用于解压
                var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);
                
                try
                {
                    // 解压ZIP文件
                    ZipFile.ExtractToDirectory(importPath, tempDirectory);
                    
                    // 读取index.json文件
                    var profileJsonPath = Path.Combine(tempDirectory, "index.json");
                    if (!File.Exists(profileJsonPath))
                    {
                        System.Diagnostics.Debug.WriteLine("ZIP文件中缺少index.json文件");
                        return null;
                    }
                    
                    var json = File.ReadAllText(profileJsonPath);
                    System.Diagnostics.Debug.WriteLine($"导入的JSON内容: {json}");
                    
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new SoundProfileJsonConverter());
                    var profile = JsonSerializer.Deserialize<SoundProfile>(json, options);
                    
                    // 验证是否正确读取了assigned_sounds
                    System.Diagnostics.Debug.WriteLine($"读取到的AssignedSounds数量: {profile?.AssignedSounds?.Count ?? 0}");
                    
                    // 检查方案名称是否已存在
                    var profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                    if (Directory.Exists(profileDirectory))
                    {
                        // 如果方案名称已存在，则添加时间戳
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        profile.Name = $"{profile.Name}_{timestamp}";
                        profileDirectory = Path.Combine(_profilesDirectory, profile.Name);
                    }
                    
                    // 创建方案目录
                    Directory.CreateDirectory(profileDirectory);
                    profile.FilePath = profileDirectory;
                    
                    // 复制sounds文件夹
                    var sourceKeySoundsDir = Path.Combine(tempDirectory, "sounds");
                    var destKeySoundsDir = Path.Combine(profileDirectory, "sounds");
                    if (Directory.Exists(sourceKeySoundsDir))
                    {
                        CopyDirectory(sourceKeySoundsDir, destKeySoundsDir);
                    }
                    
                    // 复制默认音效文件
                    var extensions = new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" };
                    foreach (var extension in extensions)
                    {
                        var defaultSoundFile = Path.Combine(tempDirectory, "DefaultSound" + extension);
                        if (File.Exists(defaultSoundFile))
                        {
                            var destDefaultSoundFile = Path.Combine(profileDirectory, "DefaultSound" + extension);
                            File.Copy(defaultSoundFile, destDefaultSoundFile);
                            profile.DefaultSound = destDefaultSoundFile;
                            break;
                        }
                    }
                    
                    // 修正KeySounds中的文件路径
                    // 只有当AssignedSounds有数据时才执行转换
                    if (profile.AssignedSounds != null && profile.AssignedSounds.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理 {profile.AssignedSounds.Count} 个分配的声音");
                        profile.KeySounds.Clear();
                        foreach (var assignment in profile.AssignedSounds)
                        {
                            System.Diagnostics.Debug.WriteLine($"处理分配的声音: Key='{assignment.Key}', Sound='{assignment.Sound}'");
                            
                            // 解析按键名称，支持数字键和其他特殊键名
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
                    
                    // 处理soundsList中的文件（其他软件可能使用这种方式存储文件列表）
                    // 如果AssignedSounds为空但有soundsList文件，则尝试从文件名推断按键
                    if ((profile.AssignedSounds == null || profile.AssignedSounds.Count == 0))
                    {
                        // 检查是否有sounds文件夹
                        if (Directory.Exists(destKeySoundsDir))
                        {
                            var soundFiles = Directory.GetFiles(destKeySoundsDir, "*.*", SearchOption.AllDirectories)
                                .Where(file => new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" }
                                    .Contains(Path.GetExtension(file).ToLowerInvariant()));
                            
                            foreach (var soundFile in soundFiles)
                            {
                                var fileName = Path.GetFileName(soundFile);
                                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(soundFile);
                                
                                // 使用增强的按键解析功能
                                var key = ParseKeyName(fileNameWithoutExt);
                                if (key.HasValue)
                                {
                                    profile.KeySounds[key.Value] = soundFile;
                                }
                            }
                        }
                    }
                    
                    // 保存并加载新方案
                    SaveProfile(profile);
                    _profiles.Add(profile);
                    
                    return profile;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入声音方案失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 复制目录及其内容
        /// </summary>
        /// <param name="sourceDir">源目录</param>
        /// <param name="destDir">目标目录</param>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            // 创建目标目录
            Directory.CreateDirectory(destDir);
            
            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile);
            }
            
            // 递归复制子目录
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDirectory);
            }
        }
    }
}
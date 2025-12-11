using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading;
using System.Windows;

namespace EKSE.Services
{
    /// <summary>
    /// 应用程序设置管理器，用于自动保存和加载设置配置文件
    /// 完全重构版本，遵循项目规范要求
    /// </summary>
    public sealed class SettingsManager
    {
        #region 字段和属性
        
        private static readonly Lazy<SettingsManager> _instance = new Lazy<SettingsManager>(() => new SettingsManager());
        private readonly string _settingsFilePath;
        private readonly object _lockObject = new object();
        private AppSettings _currentSettings;
        
        /// <summary>
        /// 获取SettingsManager的单例实例
        /// </summary>
        public static SettingsManager Instance => _instance.Value;
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 公共构造函数，允许外部实例化
        /// </summary>
        public SettingsManager()
        {
            // 设置配置文件路径为程序运行目录下的settings.json
            _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            
            // 确保目录存在
            EnsureDirectoryExists();
            
            // 初始化默认设置
            _currentSettings = CreateDefaultSettings();
        }
        
        #endregion
        
        #region 公共方法
        
        /// <summary>
        /// 加载设置配置文件
        /// 每次调用都会从磁盘重新读取配置文件，确保获取最新设置
        /// </summary>
        /// <returns>应用程序设置</returns>
        public AppSettings LoadSettings()
        {
            lock (_lockObject)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"=== 开始加载设置 ===");
                    System.Diagnostics.Debug.WriteLine($"配置文件路径: {_settingsFilePath}");
                    System.Diagnostics.Debug.WriteLine($"配置文件是否存在: {File.Exists(_settingsFilePath)}");
                    
                    if (File.Exists(_settingsFilePath))
                    {
                        string json = File.ReadAllText(_settingsFilePath);
                        System.Diagnostics.Debug.WriteLine($"读取到的JSON内容: {json}");
                        
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                            System.Diagnostics.Debug.WriteLine($"反序列化后的设置对象: {settings?.GetHashCode()}");
                            System.Diagnostics.Debug.WriteLine($"AutoStart值: {settings?.AutoStart}");
                            System.Diagnostics.Debug.WriteLine($"MinimizeToTray值: {settings?.MinimizeToTray}");
                            
                            if (settings != null)
                            {
                                _currentSettings = settings;
                                
                                // 验证和修复设置
                                ValidateAndRepairSettings(_currentSettings);
                                
                                System.Diagnostics.Debug.WriteLine($"验证后的AutoStart值: {_currentSettings.AutoStart}");
                                System.Diagnostics.Debug.WriteLine($"验证后的MinimizeToTray值: {_currentSettings.MinimizeToTray}");
                                
                                return _currentSettings;
                            }
                        }
                    }
                    
                    // 如果配置文件不存在或读取失败，则使用默认设置并保存
                    System.Diagnostics.Debug.WriteLine("使用默认设置");
                    _currentSettings = CreateDefaultSettings();
                    SaveSettingsInternal(_currentSettings);
                }
                catch (Exception ex)
                {
                    // 出现错误时使用默认设置
                    System.Diagnostics.Debug.WriteLine($"加载设置时发生异常: {ex.Message}");
                    _currentSettings = CreateDefaultSettings();
                    
                    // 尝试保存默认设置以修复可能的配置文件问题
                    SaveSettingsInternal(_currentSettings);
                }
                
                return _currentSettings;
            }
        }
        
        /// <summary>
        /// 保存设置配置文件
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        public void SaveSettings(AppSettings settings)
        {
            lock (_lockObject)
            {
                SaveSettingsInternal(settings);
            }
        }
        
        /// <summary>
        /// 获取当前设置
        /// </summary>
        /// <returns>当前应用程序设置</returns>
        public AppSettings GetCurrentSettings()
        {
            return _currentSettings;
        }
        
        /// <summary>
        /// 更新设置
        /// </summary>
        /// <param name="newSettings">新设置</param>
        public void UpdateSettings(AppSettings newSettings)
        {
            if (newSettings != null)
            {
                _currentSettings = newSettings;
                SaveSettings();
            }
        }

        /// <summary>
        /// 保存当前设置
        /// </summary>
        public void SaveSettings()
        {
            SaveSettingsInternal(_currentSettings);
        }
        
        /// <summary>
        /// 创建默认设置
        /// </summary>
        /// <returns>默认应用程序设置</returns>
        private AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                AutoStart = false,
                MinimizeToTray = false,
                ThemeColor = "#673AB7", // 默认紫色主题
                ThemeType = "Light"     // 默认浅色主题
            };
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 保存设置配置文件
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        private void SaveSettingsInternal(AppSettings settings)
        {
            lock (_lockObject)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"=== 开始保存设置 ===");
                    System.Diagnostics.Debug.WriteLine($"配置文件路径: {_settingsFilePath}");
                    System.Diagnostics.Debug.WriteLine($"AutoStart值: {settings?.AutoStart}");
                    System.Diagnostics.Debug.WriteLine($"MinimizeToTray值: {settings?.MinimizeToTray}");
                    
                    // 确保目录存在
                    EnsureDirectoryExists();
                    
                    // 序列化设置对象为JSON格式
                    var json = JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.All
                    });
                    System.Diagnostics.Debug.WriteLine($"序列化后的JSON内容: {json}");
                    
                    // 写入文件
                    File.WriteAllText(_settingsFilePath, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存设置时发生异常: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 确保配置文件所在目录存在
        /// </summary>
        private void EnsureDirectoryExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                LogError($"创建配置目录时出错: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 验证和修复设置
        /// </summary>
        /// <param name="settings">要验证的设置</param>
        private void ValidateAndRepairSettings(AppSettings settings)
        {
            // 验证主题颜色格式
            if (string.IsNullOrEmpty(settings.ThemeColor))
            {
                settings.ThemeColor = "#673AB7"; // 默认紫色主题
            }
            
            // 验证主题类型
            if (string.IsNullOrEmpty(settings.ThemeType) || 
                (settings.ThemeType != "Light" && settings.ThemeType != "Dark"))
            {
                settings.ThemeType = "Light"; // 默认浅色主题
            }
        }
        
        // 动态生成设置的方法已被简化默认设置方法取代，不再需要
        
        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="exception">异常对象</param>
        private void LogError(string message, Exception exception)
        {
            // 在调试输出中记录错误
            System.Diagnostics.Debug.WriteLine($"{message}: {exception}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 应用程序设置数据模型
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 主题颜色
        /// </summary>
        public string ThemeColor { get; set; }
        
        /// <summary>
        /// 是否开机自启
        /// </summary>
        public bool AutoStart { get; set; } = false;
        
        /// <summary>
        /// 是否最小化到托盘
        /// </summary>
        public bool MinimizeToTray { get; set; } = true;
        
        /// <summary>
        /// 音量设置
        /// </summary>
        public int Volume { get; set; }
        
        /// <summary>
        /// 主题类型（浅色/深色）
        /// </summary>
        public string ThemeType { get; set; }
    }
}
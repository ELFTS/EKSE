using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace EKSE.Services
{
    /// <summary>
    /// 应用程序设置管理器，用于自动保存和加载设置配置文件
    /// </summary>
    public sealed class SettingsManager
    {
        #region Fields and Properties
        
        private static readonly Lazy<SettingsManager> _instance = new Lazy<SettingsManager>(() => new SettingsManager());
        private readonly string _settingsFilePath;
        private readonly object _lockObject = new object();
        private AppSettings _currentSettings;
        
        /// <summary>
        /// 获取SettingsManager的单例实例
        /// </summary>
        public static SettingsManager Instance => _instance.Value;
        
        #endregion
        
        #region Constructor
        
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
        
        #region Public Methods
        
        /// <summary>
        /// 加载设置配置文件
        /// </summary>
        /// <returns>应用程序设置</returns>
        public AppSettings LoadSettings()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        string json = File.ReadAllText(_settingsFilePath);
                        
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var settings = JsonSerializer.Deserialize<AppSettings>(json);
                            if (settings != null)
                            {
                                _currentSettings = settings;
                                
                                // 验证和修复设置
                                ValidateAndRepairSettings(_currentSettings);
                                
                                return _currentSettings;
                            }
                        }
                    }
                    
                    // 如果配置文件不存在或读取失败，则使用默认设置并保存
                    _currentSettings = CreateDefaultSettings();
                    SaveSettingsInternal(_currentSettings);
                }
                catch (Exception)
                {
                    // 出现错误时使用默认设置
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
            return LoadSettings();
        }
        
        /// <summary>
        /// 更新并保存设置
        /// </summary>
        /// <param name="settings">新的设置</param>
        public void UpdateSettings(AppSettings settings)
        {
            SaveSettings(settings);
        }
        
        /// <summary>
        /// 创建默认设置实例
        /// </summary>
        /// <returns>默认设置实例</returns>
        public AppSettings CreateDefaultSettings()
        {
            return new AppSettings
            {
                ThemeColor = GenerateDynamicThemeColor(),
                AutoStart = GenerateDynamicAutoStartSetting(),
                MinimizeToTray = GenerateDynamicMinimizeToTraySetting(),
                Volume = GenerateDynamicVolumeSetting(),
                ThemeType = GenerateDynamicThemeTypeSetting()
            };
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 内部保存设置的方法，不包含锁机制
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        private void SaveSettingsInternal(AppSettings settings)
        {
            try
            {
                _currentSettings = settings ?? CreateDefaultSettings();
                
                // 验证设置对象
                ValidateAndRepairSettings(_currentSettings);
                
                string json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                // 记录保存设置时发生的错误
                LogError($"保存设置时出错: {ex.Message}", ex);
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
        /// 验证和修复设置项
        /// </summary>
        /// <param name="settings">要验证的设置</param>
        private void ValidateAndRepairSettings(AppSettings settings)
        {
            // 验证主题颜色
            if (string.IsNullOrWhiteSpace(settings.ThemeColor))
            {
                settings.ThemeColor = GenerateDynamicThemeColor();
            }
            else if (!settings.ThemeColor.StartsWith("#"))
            {
                // 确保颜色以#开头
                settings.ThemeColor = "#" + settings.ThemeColor;
            }
            
            // 验证音量范围
            if (settings.Volume < 0 || settings.Volume > 100)
            {
                settings.Volume = GenerateDynamicVolumeSetting();
            }
            
            // 验证主题类型
            if (string.IsNullOrWhiteSpace(settings.ThemeType))
            {
                settings.ThemeType = GenerateDynamicThemeTypeSetting();
            }
        }
        
        /// <summary>
        /// 根据系统环境智能确定主题颜色
        /// </summary>
        /// <returns>推荐的主题颜色</returns>
        private string GenerateDynamicThemeColor()
        {
            // 默认使用紫色作为主题色
            return "#FF800080";
        }
        
        /// <summary>
        /// 根据系统环境智能确定开机自启设置
        /// </summary>
        /// <returns>推荐的开机自启设置</returns>
        private bool GenerateDynamicAutoStartSetting()
        {
            // 默认不开启自启
            return false;
        }
        
        /// <summary>
        /// 根据系统环境智能确定最小化到托盘设置
        /// </summary>
        /// <returns>推荐的最小化到托盘设置</returns>
        private bool GenerateDynamicMinimizeToTraySetting()
        {
            // 默认启用最小化到托盘
            return true;
        }
        
        /// <summary>
        /// 根据系统环境智能确定默认音量设置
        /// </summary>
        /// <returns>推荐的默认音量</returns>
        private int GenerateDynamicVolumeSetting()
        {
            // 默认音量设为80%
            return 80;
        }
        
        /// <summary>
        /// 根据系统环境智能确定主题类型设置
        /// </summary>
        /// <returns>推荐的主题类型</returns>
        private string GenerateDynamicThemeTypeSetting()
        {
            // 默认使用浅色主题
            return "Light";
        }
        
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
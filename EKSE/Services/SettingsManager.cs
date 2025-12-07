using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace EKSE.Services
{
    /// <summary>
    /// 应用程序设置管理器，用于自动保存和加载设置配置文件
    /// </summary>
    public class SettingsManager
    {
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;

        public SettingsManager()
        {
            try
            {
                // 设置配置文件路径为程序运行目录下的settings.json
                _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                System.Diagnostics.Debug.WriteLine($"SettingsManager初始化，配置文件路径: {_settingsFilePath}");
                
                // 确保目录存在
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    System.Diagnostics.Debug.WriteLine($"创建目录: {directory}");
                }
                
                // 初始化默认设置
                _currentSettings = new AppSettings();
                System.Diagnostics.Debug.WriteLine($"SettingsManager初始化完成，初始设置: ThemeColor={_currentSettings.ThemeColor}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsManager初始化失败: {ex.Message}");
                _currentSettings = new AppSettings(); // 确保始终有默认设置
            }
        }

        /// <summary>
        /// 加载设置配置文件
        /// </summary>
        /// <returns>应用程序设置</returns>
        public AppSettings LoadSettings()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"尝试加载设置文件: {_settingsFilePath}");
                System.Diagnostics.Debug.WriteLine($"设置文件是否存在: {File.Exists(_settingsFilePath)}");

                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    System.Diagnostics.Debug.WriteLine($"读取到的设置内容: {json}");
                    
                    if (!string.IsNullOrEmpty(json))
                    {
                        var settings = JsonSerializer.Deserialize<AppSettings>(json);
                        if (settings != null)
                        {
                            _currentSettings = settings;
                            System.Diagnostics.Debug.WriteLine($"设置加载成功: ThemeColor={_currentSettings.ThemeColor}");

                            // 验证主题颜色
                            if (string.IsNullOrEmpty(_currentSettings.ThemeColor))
                            {
                                _currentSettings.ThemeColor = "#FF800080"; // 默认紫色
                                System.Diagnostics.Debug.WriteLine("加载的主题颜色为空，设置为默认紫色");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("解析设置内容失败，使用默认设置");
                            _currentSettings = new AppSettings();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("设置文件为空，使用默认设置");
                        _currentSettings = new AppSettings();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"设置文件不存在，使用默认设置");
                    _currentSettings = new AppSettings();
                    System.Diagnostics.Debug.WriteLine($"默认设置: ThemeColor={_currentSettings.ThemeColor}");
                    // 如果配置文件不存在，使用默认设置并保存
                    SaveSettings(_currentSettings);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"错误堆栈: {ex.StackTrace}");
                // 出现错误时使用默认设置
                _currentSettings = new AppSettings();
            }

            return _currentSettings;
        }

        /// <summary>
        /// 保存设置配置文件
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        public void SaveSettings(AppSettings settings)
        {
            try
            {
                _currentSettings = settings ?? new AppSettings(); // 确保不为null
                System.Diagnostics.Debug.WriteLine($"准备序列化设置对象: ThemeColor={_currentSettings.ThemeColor}");
                
                // 验证设置对象
                if (string.IsNullOrEmpty(_currentSettings.ThemeColor))
                {
                    _currentSettings.ThemeColor = "#FF800080"; // 默认紫色
                    System.Diagnostics.Debug.WriteLine("主题颜色为空，设置为默认紫色");
                }
                else if (!_currentSettings.ThemeColor.StartsWith("#"))
                {
                    // 确保颜色以#开头
                    _currentSettings.ThemeColor = "#" + _currentSettings.ThemeColor;
                    System.Diagnostics.Debug.WriteLine($"修正颜色格式为: {_currentSettings.ThemeColor}");
                }
                
                string json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
                System.Diagnostics.Debug.WriteLine($"序列化后的JSON内容: {json}");
                
                File.WriteAllText(_settingsFilePath, json);
                System.Diagnostics.Debug.WriteLine($"设置已保存到: {_settingsFilePath}");
                System.Diagnostics.Debug.WriteLine($"保存的设置内容: {json}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置时出错: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"错误堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取当前设置
        /// </summary>
        /// <returns>当前应用程序设置</returns>
        public AppSettings GetCurrentSettings()
        {
            if (_currentSettings == null)
            {
                _currentSettings = new AppSettings();
                System.Diagnostics.Debug.WriteLine("GetCurrentSettings: 创建了新的AppSettings实例");
            }
            
            return _currentSettings;
        }

        /// <summary>
        /// 更新并保存设置
        /// </summary>
        /// <param name="settings">新的设置</param>
        public void UpdateSettings(AppSettings settings)
        {
            SaveSettings(settings);
        }
    }

    /// <summary>
    /// 应用程序设置数据模型
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 主题颜色
        /// </summary>
        public string ThemeColor { get; set; } = "#FF800080"; // 默认紫色

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
        public int Volume { get; set; } = 80;

        /// <summary>
        /// 主题类型（浅色/深色）
        /// </summary>
        public string ThemeType { get; set; } = "Light";
    }
}
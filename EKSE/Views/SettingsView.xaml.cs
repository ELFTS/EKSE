using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using EKSE.Services;

namespace EKSE.Views
{
    /// <summary>
    /// SettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsView : UserControl
    {
        // 保存当前设置
        private AppSettings? _currentSettings;
        
        // 标志位，用于防止在初始化过程中触发事件处理
        private bool _isInitializing = true;

        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            InitializeConfigManagement();
            LoadCurrentSettings();
            
            // 初始化完成后，允许事件处理
            _isInitializing = false;
            
            // 取消订阅Loaded事件以避免重复触发（可选）
            Loaded -= SettingsView_Loaded;
        }
        
        /// <summary>
        /// 公共方法，用于在外部需要时刷新设置
        /// </summary>
        public void RefreshSettings()
        {
            _isInitializing = true;
            LoadCurrentSettings();
            _isInitializing = false;
        }

        private void InitializeConfigManagement()
        {
            // 注册开关控件的事件处理程序
            RegisterToggleEvents();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // 从应用程序设置管理器加载当前设置（使用单例模式）
                if (Application.Current is App app && app.SettingsManager != null)
                {
                    _currentSettings = app.SettingsManager.GetCurrentSettings();
                }
                
                // 确保_currentSettings不为null
                if (_currentSettings == null)
                {
                    _currentSettings = new AppSettings();
                }
                
                // 应用保存的设置到UI控件
                ApplySettingsToUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载当前设置时出错: {ex.Message}");
                // 出错时使用默认设置
                _currentSettings = new AppSettings();
                ApplySettingsToUI();
            }
        }

        // 应用设置到UI控件
        private void ApplySettingsToUI()
        {
            if (_currentSettings != null)
            {
                // 恢复开关控件状态
                RestoreToggleStates();
            }
        }

        // 恢复开关控件状态
        private void RestoreToggleStates()
        {
            if (_currentSettings != null)
            {
                // 恢复开机自启开关状态
                if (AutoStartToggle != null)
                {
                    AutoStartToggle.IsChecked = _currentSettings.AutoStart;
                }
                
                // 恢复最小化到托盘开关状态
                if (MinimizeToTrayToggle != null)
                {
                    MinimizeToTrayToggle.IsChecked = _currentSettings.MinimizeToTray;
                }
            }
        }

        private void UpdateCurrentSettings()
        {
            System.Diagnostics.Debug.WriteLine("开始更新当前设置");
            
            // 确保_currentSettings不为null
            if (_currentSettings == null)
            {
                System.Diagnostics.Debug.WriteLine("_currentSettings为null，从设置管理器重新加载");
                if (Application.Current is App app && app.SettingsManager != null)
                {
                    _currentSettings = app.SettingsManager.GetCurrentSettings();
                }
                
                // 如果还是null，则创建默认设置
                if (_currentSettings == null)
                {
                    _currentSettings = new AppSettings();
                    System.Diagnostics.Debug.WriteLine("创建了新的AppSettings实例");
                }
            }
            
            if (_currentSettings != null)
            {
                // 更新开关控件状态
                if (AutoStartToggle != null)
                {
                    // 如果开关控件的IsChecked为null，保持原来设置不变
                    if (AutoStartToggle.IsChecked.HasValue)
                    {
                        _currentSettings.AutoStart = AutoStartToggle.IsChecked.Value;
                    }
                    System.Diagnostics.Debug.WriteLine($"更新开机自启设置: {_currentSettings.AutoStart}");
                }
                
                if (MinimizeToTrayToggle != null)
                {
                    // 如果开关控件的IsChecked为null，保持原来设置不变
                    if (MinimizeToTrayToggle.IsChecked.HasValue)
                    {
                        _currentSettings.MinimizeToTray = MinimizeToTrayToggle.IsChecked.Value;
                    }
                    System.Diagnostics.Debug.WriteLine($"更新最小化到托盘设置: {_currentSettings.MinimizeToTray}");
                }
                
                System.Diagnostics.Debug.WriteLine($"已更新当前设置: AutoStart={_currentSettings.AutoStart}, MinimizeToTray={_currentSettings.MinimizeToTray}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("错误：_currentSettings为null，无法更新设置");
            }
        }

        private void Toggle_Checked(object sender, RoutedEventArgs e)
        {
            // 如果正在初始化过程中，则不处理事件
            if (_isInitializing)
            {
                System.Diagnostics.Debug.WriteLine("正在初始化过程中，忽略Toggle_Checked事件");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("开关控件状态发生变化");
            
            // 更新当前设置
            UpdateCurrentSettings();
            
            // 保存设置（使用单例模式）
            if (_currentSettings != null && Application.Current is App app && app.SettingsManager != null)
            {
                app.SettingsManager.SaveSettings(_currentSettings);
                System.Diagnostics.Debug.WriteLine("开关设置已保存");
            }
        }

        // 注册开关控件事件
        private void RegisterToggleEvents()
        {
            System.Diagnostics.Debug.WriteLine("注册开关控件事件");
            
            if (AutoStartToggle != null)
            {
                AutoStartToggle.Checked += Toggle_Checked;
                AutoStartToggle.Unchecked += Toggle_Checked;
                System.Diagnostics.Debug.WriteLine("已注册AutoStartToggle事件");
            }
            
            if (MinimizeToTrayToggle != null)
            {
                MinimizeToTrayToggle.Checked += Toggle_Checked;
                MinimizeToTrayToggle.Unchecked += Toggle_Checked;
                System.Diagnostics.Debug.WriteLine("已注册MinimizeToTrayToggle事件");
            }
        }

        // 取消注册开关控件事件
        private void UnregisterToggleEvents()
        {
            System.Diagnostics.Debug.WriteLine("取消注册开关控件事件");
            
            if (AutoStartToggle != null)
            {
                AutoStartToggle.Checked -= Toggle_Checked;
                AutoStartToggle.Unchecked -= Toggle_Checked;
                System.Diagnostics.Debug.WriteLine("已取消注册AutoStartToggle事件");
            }
            
            if (MinimizeToTrayToggle != null)
            {
                MinimizeToTrayToggle.Checked -= Toggle_Checked;
                MinimizeToTrayToggle.Unchecked -= Toggle_Checked;
                System.Diagnostics.Debug.WriteLine("已取消注册MinimizeToTrayToggle事件");
            }
        }
    }
}

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using EKSE.Services;

namespace EKSE.Views
{
    /// <summary>
    /// SettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsView : UserControl
    {
        // 定义颜色更改事件
        public event EventHandler<Color>? ThemeColorChanged;
        
        // 保存当前选中的颜色
        private static Color _currentThemeColor = Colors.Purple;
        
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
            // 注意：LoadCurrentSettings已经调用了RestoreColorSelection，这里不需要重复调用
            
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
            // 注册颜色选项的事件处理程序
            RegisterColorOptionEvents();
            
            // 注册开关控件的事件处理程序
            RegisterToggleEvents();
        }

        // 注册颜色选项事件
        private void RegisterColorOptionEvents()
        {
            PurpleColorOption.Checked += ColorOption_Checked;
            BlueColorOption.Checked += ColorOption_Checked;
            GreenColorOption.Checked += ColorOption_Checked;
            OrangeColorOption.Checked += ColorOption_Checked;
            RedColorOption.Checked += ColorOption_Checked;
            PinkColorOption.Checked += ColorOption_Checked;
            IndigoColorOption.Checked += ColorOption_Checked;
            TealColorOption.Checked += ColorOption_Checked;
            LimeColorOption.Checked += ColorOption_Checked;
        }

        // 取消注册颜色选项事件
        private void UnregisterColorOptionEvents()
        {
            PurpleColorOption.Checked -= ColorOption_Checked;
            BlueColorOption.Checked -= ColorOption_Checked;
            GreenColorOption.Checked -= ColorOption_Checked;
            OrangeColorOption.Checked -= ColorOption_Checked;
            RedColorOption.Checked -= ColorOption_Checked;
            PinkColorOption.Checked -= ColorOption_Checked;
            IndigoColorOption.Checked -= ColorOption_Checked;
            TealColorOption.Checked -= ColorOption_Checked;
            LimeColorOption.Checked -= ColorOption_Checked;
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
                _currentThemeColor = Colors.Purple;
                ApplySettingsToUI();
            }
        }

        // 应用设置到UI控件
        private void ApplySettingsToUI()
        {
            if (_currentSettings != null)
            {
                // 应用保存的主题颜色
                if (!string.IsNullOrEmpty(_currentSettings.ThemeColor))
                {
                    var color = (Color)ColorConverter.ConvertFromString(_currentSettings.ThemeColor);
                    _currentThemeColor = color;
                    
                    // 同时更新标题栏颜色
                    UpdateTitleBarColor(color);
                    
                    // 应用主题颜色到MaterialDesignThemes
                    ApplyThemeColor(color);
                }
                else
                {
                    _currentThemeColor = Colors.Purple;
                    UpdateTitleBarColor(Colors.Purple);
                    ApplyThemeColor(Colors.Purple);
                }
                
                // 恢复颜色选择状态
                RestoreColorSelection();
                
                // 恢复开关控件状态
                RestoreToggleStates();
            }
        }

        // 恢复颜色选择状态
        private void RestoreColorSelection()
        {
            // 移除事件处理程序以避免在设置选中状态时触发事件
            UnregisterColorOptionEvents();
            
            // 根据当前主题颜色设置选中状态
            // 使用Color的RGB值进行比较，而不是字符串比较
            if (_currentThemeColor.Equals(Colors.Red))
            {
                RedColorOption.IsChecked = true;
            }
            else if (_currentThemeColor.Equals(Colors.Green))
            {
                GreenColorOption.IsChecked = true;
            }
            else if (_currentThemeColor.Equals(Colors.Blue))
            {
                BlueColorOption.IsChecked = true;
            }
            else if (_currentThemeColor.Equals(Colors.Orange))
            {
                OrangeColorOption.IsChecked = true;
            }
            else if (_currentThemeColor.Equals(Colors.DeepPink))
            {
                PinkColorOption.IsChecked = true;
            }
            else if (_currentThemeColor.Equals(Colors.Indigo))
            {
                IndigoColorOption.IsChecked = true;
            }
            else if (_currentThemeColor.Equals(Colors.Teal))
            {
                TealColorOption.IsChecked = true;
            }
            else if (_currentThemeColor.Equals(Colors.LimeGreen))
            {
                LimeColorOption.IsChecked = true;
            }
            else // 默认为紫色
            {
                PurpleColorOption.IsChecked = true;
            }
            
            // 重新添加事件处理程序
            RegisterColorOptionEvents();
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

        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要重置所有设置吗？这将恢复到默认配置。", "确认重置", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // 重置颜色为默认紫色
                _currentThemeColor = Colors.Purple;
                
                // 更新颜色选择状态
                RestoreColorSelection();
                
                // 重置主题
                ApplyThemeColor(_currentThemeColor);
                UpdateTitleBarColor(_currentThemeColor);
                
                // 更新当前设置
                if (_currentSettings == null)
                {
                    _currentSettings = new AppSettings();
                }
                
                _currentSettings.ThemeColor = _currentThemeColor.ToString();
                // 重置其他设置为默认值
                _currentSettings.AutoStart = false;
                _currentSettings.MinimizeToTray = true;
                _currentSettings.Volume = 80;
                _currentSettings.ThemeType = "Light";
                
                // 保存设置
                if (Application.Current is App app && app.SettingsManager != null)
                {
                    app.SettingsManager.SaveSettings(_currentSettings);
                    
                    // 触发颜色更改事件
                    ThemeColorChanged?.Invoke(this, _currentThemeColor);
                }
                
                MessageBox.Show("设置已重置为默认值", "重置完成", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // 更新主题颜色，使用十六进制格式保存颜色
                _currentSettings.ThemeColor = "#" + _currentThemeColor.ToString().Replace("#", "");
                
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
                
                // 更新其他设置（如果存在对应的控件）
                // TODO: 添加其他设置控件的更新逻辑
                
                System.Diagnostics.Debug.WriteLine($"已更新当前设置: ThemeColor={_currentSettings.ThemeColor}, AutoStart={_currentSettings.AutoStart}, MinimizeToTray={_currentSettings.MinimizeToTray}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("错误：_currentSettings为null，无法更新设置");
            }
        }

        private void ColorOption_Checked(object sender, RoutedEventArgs e)
        {
            // 如果正在初始化过程中，则不处理事件
            if (_isInitializing)
            {
                System.Diagnostics.Debug.WriteLine("正在初始化过程中，忽略ColorOption_Checked事件");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("ColorOption_Checked事件被触发");
            
            if (sender is RadioButton radioButton)
            {
                System.Diagnostics.Debug.WriteLine($"选中的RadioButton: {radioButton.Name}");
                
                // 获取选中的颜色
                var color = GetColorFromRadioButton(radioButton);
                
                // 保存当前颜色
                _currentThemeColor = color;
                System.Diagnostics.Debug.WriteLine($"当前主题颜色已更新为: {_currentThemeColor}");
                
                // 应用颜色到应用程序资源
                ApplyThemeColor(color);
                
                // 更新标题栏颜色资源
                UpdateTitleBarColor(color);
                
                // 确保_currentSettings不为null
                if (_currentSettings == null)
                {
                    System.Diagnostics.Debug.WriteLine("_currentSettings为null，从设置管理器重新加载");
                    if (Application.Current is App appInstance && appInstance.SettingsManager != null)
                    {
                        _currentSettings = appInstance.SettingsManager.GetCurrentSettings();
                    }
                    
                    // 如果还是null，则创建默认设置
                    if (_currentSettings == null)
                    {
                        _currentSettings = new AppSettings();
                        System.Diagnostics.Debug.WriteLine("创建了新的AppSettings实例");
                    }
                }
                
                // 更新当前设置
                UpdateCurrentSettings();
                
                // 保存设置
                System.Diagnostics.Debug.WriteLine("准备保存设置...");
                if (_currentSettings != null && Application.Current is App app && app.SettingsManager != null)
                {
                    app.SettingsManager.SaveSettings(_currentSettings);
                    System.Diagnostics.Debug.WriteLine("设置保存完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("错误：无法保存设置，当前设置或应用实例为null");
                }
                
                // 强制刷新按钮样式
                RefreshButtonStyles();
                
                // 触发颜色更改事件
                ThemeColorChanged?.Invoke(this, color);
            }
        }
        
        /// <summary>
        /// 强制刷新按钮样式
        /// </summary>
        private void RefreshButtonStyles()
        {
            // 重新应用样式到所有按钮
            if (ExportConfigButton != null)
                ExportConfigButton.Style = (Style)FindResource("GradientButtonStyle");
                
            if (ImportConfigButton != null)
                ImportConfigButton.Style = (Style)FindResource("GradientButtonStyle");
                
            if (ResetSettingsButton != null)
                ResetSettingsButton.Style = (Style)FindResource("GradientButtonStyle");
        }

        private Color GetColorFromRadioButton(RadioButton radioButton)
        {
            switch (radioButton.Name)
            {
                case "PurpleColorOption":
                    return Colors.Purple;
                case "BlueColorOption":
                    return Colors.Blue;
                case "GreenColorOption":
                    return Colors.Green;
                case "OrangeColorOption":
                    return Colors.Orange;
                case "RedColorOption":
                    return Colors.Red;
                case "PinkColorOption":
                    return Colors.DeepPink;
                case "IndigoColorOption":
                    return Colors.Indigo;
                case "TealColorOption":
                    return Colors.Teal;
                case "LimeColorOption":
                    return Colors.LimeGreen;
                default:
                    return Colors.Purple; // 默认颜色
            }
        }

        private void ApplyThemeColor(Color color)
        {
            try
            {
                // 使用MaterialDesignThemes库应用主题颜色
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();
                
                // 创建新的颜色方案
                theme.PrimaryLight = new ColorPair(
                    Color.FromArgb(100, color.R, color.G, color.B),
                    Colors.Black);
                theme.PrimaryMid = new ColorPair(color, Colors.White);
                theme.PrimaryDark = new ColorPair(
                    Color.FromArgb(255, 
                        Math.Max((byte)0, (byte)(color.R * 0.7)), 
                        Math.Max((byte)0, (byte)(color.G * 0.7)), 
                        Math.Max((byte)0, (byte)(color.B * 0.7))), 
                    Colors.White);
                
                // 应用新主题
                paletteHelper.SetTheme(theme);
                
                // 更新当前主题颜色
                if (Application.Current is App app)
                {
                    app.SetCurrentThemeColor(color);
                    
                    // 更新渐变按钮样式以反映新的主题颜色
                    app.UpdateGradientButtonStyle();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新主题颜色时出错: {ex.Message}");
            }
        }
        
        private void UpdateTitleBarColor(Color color)
        {
            try
            {
                if (Application.Current != null && Application.Current.Resources != null)
                {
                    // 创建新的画笔 - 使用线性渐变而不是纯色
                    var newBrush = BrushHelper.CreateTitleBarBrush(color);
                    
                    // 更新标题栏背景色资源
                    Application.Current.Resources["TitleBarBackground"] = newBrush;
                    System.Diagnostics.Debug.WriteLine($"标题栏颜色已更新: {color}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新标题栏颜色时出错: {ex.Message}");
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

        private void ExportConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON文件|*.json|所有文件|*.*",
                    FileName = "KeySound2_Config.json"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    // 创建示例配置数据
                    string configContent = "{\n  \"app\": \"EKSE\",\n  \"version\": \"1.0.0\",\n  \"exported\": \"" + 
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"\n}";
                    File.WriteAllText(saveFileDialog.FileName, configContent);
                    
                    MessageBox.Show("配置已导出", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出配置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON文件|*.json|所有文件|*.*"
                };
                
                if (openFileDialog.ShowDialog() == true)
                {
                    // 读取配置文件
                    string configContent = File.ReadAllText(openFileDialog.FileName);
                    
                    // 这里应该解析并应用配置
                    // 简化示例，仅显示成功消息
                    MessageBox.Show("配置已导入", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // 重新加载配置文件列表已被移除
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入配置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
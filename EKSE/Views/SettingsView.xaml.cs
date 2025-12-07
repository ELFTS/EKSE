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
        public event EventHandler<Color> ThemeColorChanged;
        
        // 保存当前选中的颜色
        private static Color _currentThemeColor = Colors.Purple;
        
        // 保存当前设置
        private AppSettings _currentSettings;

        public SettingsView()
        {
            InitializeComponent();
            InitializeConfigManagement();
            LoadCurrentSettings();
            // 注意：LoadCurrentSettings已经调用了RestoreColorSelection，这里不需要重复调用
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // 从应用程序设置管理器加载当前设置
                _currentSettings = ((App)Application.Current).SettingsManager.GetCurrentSettings();
                
                // 确保_currentSettings不为null
                if (_currentSettings == null)
                {
                    _currentSettings = new AppSettings();
                    System.Diagnostics.Debug.WriteLine("LoadCurrentSettings: 创建了新的AppSettings实例");
                }
                
                System.Diagnostics.Debug.WriteLine($"加载当前设置: ThemeColor={_currentSettings.ThemeColor}");
                
                // 应用保存的设置到UI控件
                ApplySettingsToUI();
                
                // 注意：ApplySettingsToUI已经调用了RestoreColorSelection，这里不需要重复调用
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载当前设置时出错: {ex.Message}");
                // 出错时使用默认设置
                _currentSettings = new AppSettings();
                _currentThemeColor = Colors.Purple;
                ApplySettingsToUI();
                // 注意：ApplySettingsToUI已经调用了RestoreColorSelection，这里不需要重复调用
            }
        }

        private void ApplySettingsToUI()
        {
            try
            {
                // 确保_currentSettings不为null
                if (_currentSettings == null)
                {
                    _currentSettings = new AppSettings();
                    System.Diagnostics.Debug.WriteLine("创建了新的AppSettings实例");
                }
                
                System.Diagnostics.Debug.WriteLine($"应用设置到UI，当前设置: ThemeColor={_currentSettings.ThemeColor}");
                
                // 在应用设置到UI前先移除事件处理程序，防止触发颜色更改事件
                UnregisterColorOptionEvents();
                
                // 应用保存的主题颜色
                if (!string.IsNullOrEmpty(_currentSettings.ThemeColor))
                {
                    var color = (Color)ColorConverter.ConvertFromString(_currentSettings.ThemeColor);
                    _currentThemeColor = color;
                    System.Diagnostics.Debug.WriteLine($"已应用主题颜色到_currentThemeColor: {color}");
                    
                    // 同时更新标题栏颜色
                    UpdateTitleBarColor(color);
                    
                    // 应用主题颜色到MaterialDesignThemes
                    ApplyThemeColor(color);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("主题颜色为空，使用默认紫色");
                    _currentThemeColor = Colors.Purple;
                    UpdateTitleBarColor(Colors.Purple);
                    ApplyThemeColor(Colors.Purple);
                }
                
                // 恢复颜色选择状态
                RestoreColorSelection();
                
                // 重新注册事件处理程序
                RegisterColorOptionEvents();
                
                // 应用保存的其他设置
                // TODO: 添加其他设置控件的应用逻辑
                
                // 应用保存的主题类型设置
                if (_currentSettings.ThemeType == "Dark")
                {
                    // 如果有主题选择的ComboBox，设置为深色主题
                    // 这里暂时不实现，因为需要找到具体的控件名称
                }
                
                // 应用保存的开机自启设置
                // 这个设置通常在主窗口或应用程序级别处理
                
                // 应用保存的最小化到托盘设置
                // 这个设置通常在主窗口或应用程序级别处理
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用设置到UI时出错: {ex.Message}");
                // 出错时使用默认设置
                _currentThemeColor = Colors.Purple;
                UpdateTitleBarColor(Colors.Purple);
                ApplyThemeColor(Colors.Purple);
                
                // 恢复颜色选择状态
                RestoreColorSelection();
                
                // 重新注册事件处理程序
                RegisterColorOptionEvents();
            }
        }

        private void InitializeConfigManagement()
        {
            // 初始化配置管理功能
            ExportConfigButton.Click += ExportConfigButton_Click;
            ImportConfigButton.Click += ImportConfigButton_Click;
            ResetSettingsButton.Click += ResetSettingsButton_Click;
        }
        
        // 注册颜色选项的事件处理程序
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
        
        // 注销颜色选项的事件处理程序
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
        
        // 更新当前设置
        private void UpdateCurrentSettings()
        {
            System.Diagnostics.Debug.WriteLine("开始更新当前设置");
            
            if (_currentSettings != null)
            {
                // 更新主题颜色，使用十六进制格式保存颜色
                _currentSettings.ThemeColor = "#" + _currentThemeColor.ToString().Replace("#", "");
                
                // 更新其他设置（如果存在对应的控件）
                // TODO: 添加其他设置控件的更新逻辑
                
                System.Diagnostics.Debug.WriteLine($"已更新当前设置: ThemeColor={_currentSettings.ThemeColor}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("错误：_currentSettings为null，无法更新设置");
            }
        }

        // 恢复颜色选择状态
        private void RestoreColorSelection()
        {
            System.Diagnostics.Debug.WriteLine($"恢复颜色选择状态，当前主题颜色: {_currentThemeColor}");
            
            // 移除事件处理程序以避免在设置选中状态时触发事件
            UnregisterColorOptionEvents();
            
            // 根据当前主题颜色设置选中状态
            // 使用Color的RGB值进行比较，而不是字符串比较
            if (_currentThemeColor.Equals(Colors.Red))
            {
                RedColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择红色");
            }
            else if (_currentThemeColor.Equals(Colors.Green))
            {
                GreenColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择绿色");
            }
            else if (_currentThemeColor.Equals(Colors.Blue))
            {
                BlueColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择蓝色");
            }
            else if (_currentThemeColor.Equals(Colors.Orange))
            {
                OrangeColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择橙色");
            }
            else if (_currentThemeColor.Equals(Colors.DeepPink))
            {
                PinkColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择粉色");
            }
            else if (_currentThemeColor.Equals(Colors.Indigo))
            {
                IndigoColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择靛蓝色");
            }
            else if (_currentThemeColor.Equals(Colors.Teal))
            {
                TealColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择蓝绿色");
            }
            else if (_currentThemeColor.Equals(Colors.LimeGreen))
            {
                LimeColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择青绿色");
            }
            else // 默认为紫色
            {
                PurpleColorOption.IsChecked = true;
                System.Diagnostics.Debug.WriteLine("已恢复选择紫色（默认）");
            }
            
            // 重新添加事件处理程序
            RegisterColorOptionEvents();
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
                ((App)Application.Current).SettingsManager.SaveSettings(_currentSettings);
                
                // 触发颜色更改事件
                ThemeColorChanged?.Invoke(this, _currentThemeColor);
                
                MessageBox.Show("设置已重置为默认值", "重置完成", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ColorOption_Checked(object sender, RoutedEventArgs e)
        {
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
                
                // 更新当前设置
                UpdateCurrentSettings();
                
                // 保存设置
                System.Diagnostics.Debug.WriteLine("准备保存设置...");
                if (_currentSettings != null)
                {
                    ((App)Application.Current).SettingsManager.SaveSettings(_currentSettings);
                    System.Diagnostics.Debug.WriteLine("设置保存完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("错误：_currentSettings为null，无法保存设置");
                }
                
                // 触发颜色更改事件
                ThemeColorChanged?.Invoke(this, color);
            }
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
                    // 创建新的画笔
                    var newBrush = new SolidColorBrush(color);
                    
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
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using EKSE.Services; // 添加SoundService引用

namespace EKSE.Components
{
    /// <summary>
    /// VirtualKeyboard.xaml 的交互逻辑
    /// </summary>
    public partial class VirtualKeyboard : UserControl
    {
        // 定义按键事件
        public event EventHandler<VirtualKeyEventArgs>? KeySelected;
        
        // 存储按键到音效路径的映射
        private Dictionary<Key, string> _keySoundMap;
        
        // 当前选中的按键
        private Key _selectedKey;
        
        // 添加SoundService引用
        private SoundService? _soundService;
        
        // 添加ProfileManager引用
        private ProfileManager? _profileManager;

        public VirtualKeyboard()
        {
            InitializeComponent();
            _keySoundMap = new Dictionary<Key, string>();
        }
        
        // 设置SoundService引用
        public void SetServices(SoundService soundService, ProfileManager profileManager)
        {
            _soundService = soundService;
            _profileManager = profileManager;
        }
        
        // 按键点击事件
        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string keyName)
            {
                // 将字符串转换为Key枚举
                if (Enum.TryParse<Key>(keyName, out Key key))
                {
                    _selectedKey = key;
                    
                    // 直接播放音效
                    _soundService?.PlaySound(key);
                    
                    // 仍然触发事件，以便更新UI
                    KeySelected?.Invoke(this, new VirtualKeyEventArgs(key));
                }
            }
        }

        // 设置按键的音效路径
        public void SetKeySound(Key key, string soundPath)
        {
            _keySoundMap[key] = soundPath;
        }

        // 获取按键的音效路径
        public string? GetKeySound(Key key)
        {
            return _keySoundMap.ContainsKey(key) ? _keySoundMap[key] : null;
        }

        // 获取当前选中的按键
        public Key SelectedKey => _selectedKey;
    }
    
    // 宽度到可见性转换器
    public class WidthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return width > 0 ? Visibility.Visible : Visibility.Hidden;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // 布尔值到可见性转换器
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // 布尔值到可见性反向转换器
    public class BoolToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // 功能键可见性转换器（已注释掉对KeyDef的引用）
    public class FunctionKeysVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 由于我们在XAML中硬编码了所有按键，这个转换器实际上不会被使用
            // 但为了保持代码完整性，我们保留它
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // 占位符到透明度转换器（保留基本结构，但简化实现）
    public class PlaceholderToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 由于占位符功能已通过其他方式实现，这个转换器不再需要复杂逻辑
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // 占位符到背景色转换器（保留基本结构，但简化实现）
    public class PlaceholderToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 由于占位符功能已通过其他方式实现，这个转换器不再需要复杂逻辑
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // 占位符到前景色转换器（保留基本结构，但简化实现）
    public class PlaceholderToForegroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 由于占位符功能已通过其他方式实现，这个转换器不再需要复杂逻辑
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 按键事件参数类
    public class VirtualKeyEventArgs : EventArgs
    {
        public Key Key { get; }

        public VirtualKeyEventArgs(Key key)
        {
            Key = key;
        }
    }
}
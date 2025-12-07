using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

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
        
        // 键盘行数据集合
        private ObservableCollection<KeyboardRow> _keyboardRows = new ObservableCollection<KeyboardRow>();

        public VirtualKeyboard()
        {
            InitializeComponent();
            _keySoundMap = new Dictionary<Key, string>();
            
            // 注册转换器
            Resources.Add("WidthToVisibilityConverter", new WidthToVisibilityConverter());
            Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());
            Resources.Add("BoolToVisibilityInverseConverter", new BoolToVisibilityInverseConverter());
            Resources.Add("FunctionKeysVisibilityConverter", new FunctionKeysVisibilityConverter());
            
            InitializeKeyboard();
        }
        
        private void InitializeKeyboard()
        {
            // 计算基于U单位的按键宽度 (1U = 19.05mm ≈ 40 pixels at 96 DPI)
            // 实际测试中我们使用40px作为1U的近似值，以便在屏幕上获得合适的效果
            const double U = 40.0;
            
            _keyboardRows = new ObservableCollection<KeyboardRow>
            {
                // 第一行: ESC, F1-F12
                new KeyboardRow(new List<KeyDef>
                {
                    new KeyDef("Esc", 1 * U, "Escape", 12),
                    new KeyDef("F1", 1 * U, "F1", 12),
                    new KeyDef("F2", 1 * U, "F2", 12),
                    new KeyDef("F3", 1 * U, "F3", 12),
                    new KeyDef("F4", 1 * U, "F4", 12),
                    new KeyDef("F5", 1 * U, "F5", 12),
                    new KeyDef("F6", 1 * U, "F6", 12),
                    new KeyDef("F7", 1 * U, "F7", 12),
                    new KeyDef("F8", 1 * U, "F8", 12),
                    new KeyDef("F9", 1 * U, "F9", 12),
                    new KeyDef("F10", 1 * U, "F10", 12),
                    new KeyDef("F11", 1 * U, "F11", 12),
                    new KeyDef("F12", 1 * U, "F12", 12)
                })
                {
                    GapWidth = 1 * U  // 添加空隙宽度
                },
                
                // 第二行: 波浪号, 1-0, -, =, Backspace 和 编辑控制区第一行
                new KeyboardRow(
                    new List<KeyDef>
                    {
                        new KeyDef("~\n`", 1 * U, "Oem3", 10),
                        new KeyDef("!\n1", 1 * U, "D1", 10),
                        new KeyDef("@\n2", 1 * U, "D2", 10),
                        new KeyDef("#\n3", 1 * U, "D3", 10),
                        new KeyDef("$\n4", 1 * U, "D4", 10),
                        new KeyDef("%\n5", 1 * U, "D5", 10),
                        new KeyDef("^\n6", 1 * U, "D6", 10),
                        new KeyDef("&\n7", 1 * U, "D7", 10),
                        new KeyDef("*\n8", 1 * U, "D8", 10),
                        new KeyDef("(\n9", 1 * U, "D9", 10),
                        new KeyDef(")\n0", 1 * U, "D0", 10),
                        new KeyDef("_\n-", 1 * U, "OemMinus", 10),
                        new KeyDef("+\n=", 1 * U, "OemPlus", 10),
                        new KeyDef("Backspace", 2 * U, "Back", 10)
                    },
                    new List<KeyDef>
                    {
                        new KeyDef("PrtScn", 1 * U, "PrintScreen", 10),
                        new KeyDef("ScrLk", 1 * U, "Scroll", 10),
                        new KeyDef("Pause", 1 * U, "Pause", 10)
                    }
                ),
                
                // 第三行: Tab, Q-P, [, ], \ 和 编辑控制区第二行
                new KeyboardRow(
                    new List<KeyDef>
                    {
                        new KeyDef("Tab", 1.5 * U, "Tab", 10),
                        new KeyDef("Q", 1 * U, "Q", 12),
                        new KeyDef("W", 1 * U, "W", 12),
                        new KeyDef("E", 1 * U, "E", 12),
                        new KeyDef("R", 1 * U, "R", 12),
                        new KeyDef("T", 1 * U, "T", 12),
                        new KeyDef("Y", 1 * U, "Y", 12),
                        new KeyDef("U", 1 * U, "U", 12),
                        new KeyDef("I", 1 * U, "I", 12),
                        new KeyDef("O", 1 * U, "O", 12),
                        new KeyDef("P", 1 * U, "P", 12),
                        new KeyDef("{\n[", 1 * U, "OemOpenBrackets", 10),
                        new KeyDef("}\n]", 1 * U, "OemCloseBrackets", 10),
                        new KeyDef("|\\\n\\", 1.5 * U, "Oem5", 10)
                    },
                    new List<KeyDef>
                    {
                        new KeyDef("Ins", 1 * U, "Insert", 10),
                        new KeyDef("Home", 1 * U, "Home", 10),
                        new KeyDef("PgUp", 1 * U, "PageUp", 10)
                    }
                ),
                
                // 第四行: Caps Lock, A-L, ;, ', Enter 和 编辑控制区第三行
                new KeyboardRow(
                    new List<KeyDef>
                    {
                        new KeyDef("Caps Lock", 1.75 * U, "CapsLock", 10),
                        new KeyDef("A", 1 * U, "A", 12),
                        new KeyDef("S", 1 * U, "S", 12),
                        new KeyDef("D", 1 * U, "D", 12),
                        new KeyDef("F", 1 * U, "F", 12),
                        new KeyDef("G", 1 * U, "G", 12),
                        new KeyDef("H", 1 * U, "H", 12),
                        new KeyDef("J", 1 * U, "J", 12),
                        new KeyDef("K", 1 * U, "K", 12),
                        new KeyDef("L", 1 * U, "L", 12),
                        new KeyDef(";\n:", 1 * U, "OemSemicolon", 10),
                        new KeyDef("'\n\"", 1 * U, "OemQuotes", 10),
                        new KeyDef("Enter", 2.25 * U, "Enter", 10)
                    },
                    new List<KeyDef>
                    {
                        new KeyDef("Del", 1 * U, "Delete", 10),
                        new KeyDef("End", 1 * U, "End", 10),
                        new KeyDef("PgDn", 1 * U, "PageDown", 10)
                    }
                ),
                
                // 第五行: Shift, Z-M, ,, ., /, Shift 和 方向键
                new KeyboardRow(
                    new List<KeyDef>
                    {
                        new KeyDef("Shift", 2.25 * U, "LeftShift", 10),
                        new KeyDef("Z", 1 * U, "Z", 12),
                        new KeyDef("X", 1 * U, "X", 12),
                        new KeyDef("C", 1 * U, "C", 12),
                        new KeyDef("V", 1 * U, "V", 12),
                        new KeyDef("B", 1 * U, "B", 12),
                        new KeyDef("N", 1 * U, "N", 12),
                        new KeyDef("M", 1 * U, "M", 12),
                        new KeyDef(",\n<", 1 * U, "OemComma", 10),
                        new KeyDef(".\n>", 1 * U, "OemPeriod", 10),
                        new KeyDef("/\n?", 1 * U, "OemQuestion", 10),
                        new KeyDef("Shift", 2.75 * U, "RightShift", 10)
                    },
                    new List<KeyDef>
                    {
                        new KeyDef("↑", 1 * U, "Up", 14)
                    }
                ),
                
                // 第六行: Ctrl, Win, Alt, Space, Alt, Win, Menu, Ctrl 和 方向键
                new KeyboardRow(
                    new List<KeyDef>
                    {
                        new KeyDef("Ctrl", 1.25 * U, "LeftCtrl", 10),
                        new KeyDef("Win", 1.25 * U, "LWin", 10),
                        new KeyDef("Alt", 1.25 * U, "LeftAlt", 10),
                        new KeyDef("SPACE", 6.25 * U, "Space", 10),
                        new KeyDef("Alt", 1.25 * U, "RightAlt", 10),
                        new KeyDef("Win", 1.25 * U, "RWin", 10),
                        new KeyDef("Menu", 1.25 * U, "Apps", 10),
                        new KeyDef("Ctrl", 1.25 * U, "RightCtrl", 10)
                    },
                    new List<KeyDef>
                    {
                        new KeyDef("←", 1 * U, "Left", 14),
                        new KeyDef("↓", 1 * U, "Down", 14),
                        new KeyDef("→", 1 * U, "Right", 14)
                    }
                )
                // 编辑控制区第四行: 方向键 (↑↓←→)
                /*new KeyboardRow(true, new List<KeyDef>
                {
                    new KeyDef("↑", 1 * U, "Up", 14),
                    new KeyDef("←", 1 * U, "Left", 14),
                    new KeyDef("↓", 1 * U, "Down", 14),
                    new KeyDef("→", 1 * U, "Right", 14)
                })*/
            };
            
            this.KeyboardItemsControl.ItemsSource = _keyboardRows;
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
        public static readonly WidthToVisibilityConverter Instance = new WidthToVisibilityConverter();
        
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
        public static readonly BoolToVisibilityConverter Instance = new BoolToVisibilityConverter();
        
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
        public static readonly BoolToVisibilityInverseConverter Instance = new BoolToVisibilityInverseConverter();
        
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
    
    // 功能键可见性转换器
    public class FunctionKeysVisibilityConverter : IValueConverter
    {
        public static readonly FunctionKeysVisibilityConverter Instance = new FunctionKeysVisibilityConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果功能键列表不为空且包含元素，则显示功能键容器
            if (value is IList<KeyDef> functionKeys && functionKeys.Count > 0)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 键盘行类
    public class KeyboardRow
    {
        public List<KeyDef> Keys { get; }
        public List<KeyDef> FunctionKeys { get; } // 新增功能键列表
        public bool IsDirectionalRow { get; } // 是否为方向键行
        public double GapWidth { get; set; } // 空隙宽度
        
        // 普通行构造函数
        public KeyboardRow(List<KeyDef> keys)
        {
            Keys = keys;
            FunctionKeys = new List<KeyDef>(); // 默认为空列表
            IsDirectionalRow = false;
            GapWidth = 0; // 默认无空隙
        }
        
        // 带功能键的行构造函数
        public KeyboardRow(List<KeyDef> keys, List<KeyDef> functionKeys)
        {
            Keys = keys;
            FunctionKeys = functionKeys;
            IsDirectionalRow = false;
            GapWidth = 0; // 默认无空隙
        }
        
        // 方向键行构造函数
        public KeyboardRow(bool isDirectionalRow, List<KeyDef> keys)
        {
            Keys = keys;
            FunctionKeys = new List<KeyDef>(); // 方向键行不需要功能键
            IsDirectionalRow = isDirectionalRow;
            GapWidth = 0; // 默认无空隙
        }
    }
    
    // 按键定义类
    public class KeyDef
    {
        public string Content { get; }
        public double Width { get; }
        public string Tag { get; }
        public int FontSize { get; }
        public bool IsPlaceholder { get; }

        public KeyDef(string content, double width, string tag, int fontSize)
        {
            // 始终保持内容非空，如果是占位符则设置特殊标记
            Content = content ?? "";
            Width = width;
            Tag = tag;
            FontSize = fontSize;
            IsPlaceholder = string.IsNullOrEmpty(content) || width <= 0;
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
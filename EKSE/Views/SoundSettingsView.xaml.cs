using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using EKSE.Components;
using EKSE.Services;
using EKSE.Models;
using Microsoft.VisualBasic;

namespace EKSE.Views
{
    /// <summary>
    /// SoundSettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class SoundSettingsView : UserControl
    {
        // 音频文件信息类
        public class AudioFileItem
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
        }
        
        // 服务和管理器引用
        private SoundService? _soundService;
        private ProfileManager? _profileManager;
        private AudioFileManager? _audioFileManager;
        
        // 当前选中的按键
        private System.Windows.Input.Key _selectedKey = System.Windows.Input.Key.None;
        
        // 音频文件列表
        private ObservableCollection<AudioFileItem> _audioFilesList = new ObservableCollection<AudioFileItem>();

        public SoundSettingsView()
        {
            InitializeComponent();
            // 设置ListBox的数据源
            AudioFilesListBox.ItemsSource = _audioFilesList;
        }

        // 当虚拟键盘上的按键被选中时
        private void VirtualKeyboardControl_KeySelected(object sender, VirtualKeyEventArgs e)
        {
            _selectedKey = e.Key;
            SelectedKeyText.Text = $"当前选中按键: {e.Key}";
            
            // 显示当前按键的音效路径
            UpdateSoundPathDisplay();
        }

        // 更新当前音效路径显示
        private void UpdateSoundPathDisplay()
        {
            if (_selectedKey != System.Windows.Input.Key.None && _profileManager != null)
            {
                var soundPath = _profileManager.GetKeySound(_selectedKey);
                if (!string.IsNullOrEmpty(soundPath) && File.Exists(soundPath))
                {
                    CurrentSoundPathText.Text = $"当前音效路径: {soundPath}";
                }
                else
                {
                    CurrentSoundPathText.Text = "当前音效路径: 无";
                }
            }
            else
            {
                CurrentSoundPathText.Text = "当前音效路径: 无";
            }
        }
        
        // 设置服务引用
        public void SetServices(SoundService soundService, ProfileManager profileManager, AudioFileManager audioFileManager)
        {
            _soundService = soundService;
            _profileManager = profileManager;
            _audioFileManager = audioFileManager;
            
            // 初始化声音方案界面
            InitializeProfileUI();
            
            // 设置虚拟键盘的服务引用
            VirtualKeyboardControl.SetServices(_soundService, _profileManager);
            
            // 刷新音频文件列表
            RefreshAudioFilesList();
        }
        
        // 初始化声音方案界面
        private void InitializeProfileUI()
        {
            if (_profileManager != null)
            {
                // 设置声音方案下拉框的数据源
                ProfileComboBox.ItemsSource = _profileManager.Profiles;
                ProfileComboBox.DisplayMemberPath = "Name";
                ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
            }
        }
        
        // 声音方案下拉框选择变更事件
        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_profileManager != null && ProfileComboBox.SelectedItem is SoundProfile profile)
            {
                _profileManager.SetCurrentProfile(profile);
            }
        }
        
        // 新建声音方案按钮点击事件
        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null) return;
            
            var inputDialog = new Window
            {
                Title = "新建声音方案",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var textBlock = new TextBlock
            {
                Text = "请输入方案名称:",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetRow(textBlock, 0);

            var textBox = new TextBox
            {
                Margin = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(textBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button
            {
                Content = "确定",
                Width = 75,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 75
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(textBlock);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            inputDialog.Content = grid;

            bool dialogResult = false;
            okButton.Click += (s, args) =>
            {
                dialogResult = true;
                inputDialog.Close();
            };

            cancelButton.Click += (s, args) =>
            {
                inputDialog.Close();
            };

            inputDialog.ShowDialog();

            if (dialogResult && !string.IsNullOrWhiteSpace(textBox.Text) && _profileManager != null)
            {
                var newProfile = _profileManager.CreateProfile(textBox.Text.Trim());
                ProfileComboBox.SelectedItem = newProfile;
            }
        }
        
        // 删除声音方案按钮点击事件
        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager != null && ProfileComboBox.SelectedItem is SoundProfile profile)
            {
                var result = MessageBox.Show($"确定要删除声音方案 \"{profile.Name}\" 吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _profileManager.DeleteProfile(profile);
                        ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
                        
                        // 刷新界面
                        InitializeProfileUI();
                        
                        // 刷新音频文件列表
                        RefreshAudioFilesList();
                        
                        MessageBox.Show("方案删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除方案时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        // 导出声音方案按钮点击事件
        private void ExportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager != null && ProfileComboBox.SelectedItem is SoundProfile profile)
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "ZIP文件|*.zip",
                    FileName = $"{profile.Name}.zip"
                };
                
                if (saveFileDialog.ShowDialog() == true)
                {
                    var success = _profileManager.ExportProfile(profile, saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("声音方案导出成功！", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("声音方案导出失败！", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        // 导入声音方案按钮点击事件
        private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "ZIP文件|*.zip"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                var importedProfile = _profileManager?.ImportProfile(openFileDialog.FileName);
                if (importedProfile != null)
                {
                    ProfileComboBox.SelectedItem = importedProfile;
                    MessageBox.Show("声音方案导入成功！", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("声音方案导入失败！", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 添加音频文件按钮点击事件
        private void AddAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "音频文件|*.wav;*.mp3;*.aac;*.wma;*.flac",
                Multiselect = true
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var filePath in openFileDialog.FileNames)
                {
                    _audioFileManager?.AddAudioFile(filePath);
                }
                
                RefreshAudioFilesList();
            }
        }
        
        // 刷新音频文件列表按钮点击事件
        private void RefreshAudioFilesButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudioFilesList();
        }
        
        // 刷新音频文件列表
        private void RefreshAudioFilesList()
        {
            _audioFilesList.Clear();
            
            var audioFiles = _audioFileManager?.AudioFiles;
            if (audioFiles != null)
            {
                foreach (var file in audioFiles)
                {
                    var fileInfo = new FileInfo(file);
                    var sizeString = FormatFileSize(fileInfo.Length);
                    
                    _audioFilesList.Add(new AudioFileItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        Size = sizeString
                    });
                }
            }
        }
        
        // 格式化文件大小
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
        
        // 音频文件列表双击事件
        private void AudioFilesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AudioFilesListBox.SelectedItem is AudioFileItem selectedItem && 
                _selectedKey != System.Windows.Input.Key.None &&
                _profileManager != null)
            {
                // 将选中的音频文件设置为当前按键的音效
                _profileManager.SetKeySound(_selectedKey, selectedItem.Path);
                
                // 更新显示
                UpdateSoundPathDisplay();
                
                MessageBox.Show($"已将 \"{selectedItem.Name}\" 设置为按键 {_selectedKey} 的音效", 
                    "设置成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        // 重命名音频文件按钮点击事件
        private void RenameAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is AudioFileItem item &&
                _audioFileManager != null)
            {
                var newName = Interaction.InputBox("请输入新的文件名:", "重命名", item.Name);
                if (!string.IsNullOrWhiteSpace(newName) && newName != item.Name)
                {
                    try
                    {
                        // 注意：这里需要实现重命名逻辑
                        MessageBox.Show("此功能尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshAudioFilesList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        // 删除音频文件按钮点击事件
        private void DeleteAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is AudioFileItem item &&
                _audioFileManager != null)
            {
                var result = MessageBox.Show($"确定要删除音频文件 \"{item.Name}\" 吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _audioFileManager.DeleteAudioFile(item.Path);
                        RefreshAudioFilesList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
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
using NAudio.Wave;

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
        
        private readonly ObservableCollection<AudioFileItem> _audioFilesList = new ObservableCollection<AudioFileItem>();
        
        // 服务引用
        private SoundService? _soundService;
        private ProfileManager? _profileManager;
        private AudioFileManager? _audioFileManager;
        
        private Key _selectedKey = Key.None;
        
        public SoundSettingsView()
        {
            InitializeComponent();
            AudioFilesListBox.ItemsSource = _audioFilesList;
            
            // 订阅卸载事件以清理资源
            Unloaded += SoundSettingsView_Unloaded;
        }
        
        // 用户控件卸载时取消订阅事件
        private void SoundSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消订阅所有事件以防止内存泄漏
            if (_profileManager != null)
            {
                _profileManager.ProfilesChanged -= OnProfilesChanged;
                _profileManager.CurrentProfileChanged -= OnCurrentProfileChanged;
            }
            
            if (_audioFileManager != null)
            {
                _audioFileManager.AudioFilesChanged -= OnAudioFilesChanged;
            }
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
            
            // 订阅方案变化事件
            if (_profileManager != null)
            {
                _profileManager.ProfilesChanged += OnProfilesChanged;
                _profileManager.CurrentProfileChanged += OnCurrentProfileChanged;
            }
            
            // 订阅音频文件变化事件
            if (_audioFileManager != null)
            {
                _audioFileManager.AudioFilesChanged += OnAudioFilesChanged;
            }
            
            // 初始化声音方案界面
            InitializeProfileUI();
            
            // 设置虚拟键盘的服务引用
            VirtualKeyboardControl.SetServices(_soundService, _profileManager);
            
            // 刷新音频文件列表
            RefreshAudioFilesList();
        }
        
        // 当声音方案列表发生变化时的处理方法
        private void OnProfilesChanged(object sender, EventArgs e)
        {
            // 在UI线程上刷新界面
            if (Dispatcher.CheckAccess())
            {
                InitializeProfileUI();
                // 刷新虚拟键盘的视觉状态
                VirtualKeyboardControl.RefreshVisualState();
                // 刷新音频文件列表
                RefreshAudioFilesList();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    InitializeProfileUI();
                    // 刷新虚拟键盘的视觉状态
                    VirtualKeyboardControl.RefreshVisualState();
                    // 刷新音频文件列表
                    RefreshAudioFilesList();
                });
            }
        }
        
        /// <summary>
        /// 当前方案改变时的事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCurrentProfileChanged(object sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                // 刷新界面
                InitializeProfileUI();
                VirtualKeyboardControl.RefreshVisualState();
                RefreshAudioFilesList();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    InitializeProfileUI();
                    VirtualKeyboardControl.RefreshVisualState();
                    RefreshAudioFilesList();
                });
            }
        }
        
        // 当音频文件列表发生变化时的处理方法
        private void OnAudioFilesChanged(object sender, EventArgs e)
        {
            // 在UI线程上刷新界面
            if (Dispatcher.CheckAccess())
            {
                RefreshAudioFilesList();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    RefreshAudioFilesList();
                });
            }
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
                
                // 刷新虚拟键盘的视觉状态
                VirtualKeyboardControl.RefreshVisualState();
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
        
        // 重命名声音方案按钮点击事件
        private void RenameProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || _profileManager.CurrentProfile == null)
                return;

            var currentProfile = _profileManager.CurrentProfile;
            var newName = Interaction.InputBox("请输入新的方案名称:", "重命名声音方案", currentProfile.Name);
            
            if (!string.IsNullOrWhiteSpace(newName) && newName != currentProfile.Name)
            {
                // 检查名称是否合法
                var invalidChars = Path.GetInvalidFileNameChars();
                if (newName.IndexOfAny(invalidChars) >= 0)
                {
                    MessageBox.Show("方案名称包含非法字符，请重新输入。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_profileManager.RenameProfile(currentProfile, newName))
                {
                    MessageBox.Show("重命名成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    // 强制刷新界面
                    Dispatcher.Invoke(() =>
                    {
                        // 重新设置数据源以确保更新
                        ProfileComboBox.ItemsSource = null;
                        ProfileComboBox.ItemsSource = _profileManager.Profiles;
                        ProfileComboBox.DisplayMemberPath = "Name";
                        ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
                        
                        // 刷新音频文件列表以反映新的路径
                        RefreshAudioFilesList();
                        
                        // 刷新音频文件管理器状态
                        _audioFileManager?.Refresh();
                        
                        // 刷新声音服务状态
                        _soundService?.Refresh();
                        
                        // 刷新虚拟键盘的视觉状态
                        VirtualKeyboardControl.RefreshVisualState();
                    });
                }
                else
                {
                    MessageBox.Show("重命名失败，可能是由于权限不足或其他系统错误。请确保程序有足够的权限访问方案文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 删除声音方案按钮点击事件
        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager != null && ProfileComboBox.SelectedItem is SoundProfile profile)
            {
                // 不允许删除默认方案（前端保护）
                if (profile.Name == "默认方案")
                {
                    MessageBox.Show("不能删除默认方案。", "操作不允许", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 确认删除操作
                var result = MessageBox.Show(
                    $"确定要删除声音方案 \"{profile.Name}\" 吗？此操作不可撤销。", 
                    "确认删除", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 执行删除操作
                        _profileManager.DeleteProfile(profile);
                        
                        // 刷新界面
                        InitializeProfileUI();
                        
                        // 设置当前选中项为新的当前方案
                        ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
                        
                        // 刷新音频文件列表和其他相关组件
                        RefreshAudioFilesList();
                        VirtualKeyboardControl.RefreshVisualState();
                        
                        // 通知用户删除成功
                        MessageBox.Show(
                            "声音方案已成功删除。", 
                            "删除成功", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        // 处理删除过程中的异常
                        MessageBox.Show(
                            $"删除方案时发生错误: {ex.Message}", 
                            "删除失败", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                // 如果没有选中方案，给出提示
                MessageBox.Show(
                    "请先选择一个要删除的声音方案。", 
                    "未选择方案", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
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
                    // 确保界面刷新
                    InitializeProfileUI();
                    ProfileComboBox.SelectedItem = importedProfile;
                    VirtualKeyboardControl.RefreshVisualState();
                    RefreshAudioFilesList();
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
                
                // 列表会通过AudioFilesChanged事件自动刷新，不需要手动调用
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
            
            // 直接从AudioFileManager获取文件列表，避免递归调用Refresh
            var audioFiles = _audioFileManager?.AudioFiles;
            if (audioFiles != null)
            {
                foreach (var file in audioFiles)
                {
                    var fileInfo = new FileInfo(file);
                    var sizeString = FormatFileSize(fileInfo.Length);
                    
                    _audioFilesList.Add(new AudioFileItem
                    {
                        Name = Path.GetFileName(file),  // 显示完整文件名（包含扩展名）
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
                
                // 刷新虚拟键盘的视觉状态
                VirtualKeyboardControl.RefreshVisualState();
                
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
                        var newFilePath = _audioFileManager.RenameAudioFile(item.Path, newName);
                        if (newFilePath != null)
                        {
                            MessageBox.Show("重命名成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshAudioFilesList();
                        }
                        else
                        {
                            MessageBox.Show("重命名失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
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
        
        // 播放音频文件按钮点击事件
        private void PlayAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is AudioFileItem item &&
                _soundService != null)
            {
                try
                {
                    // 使用SoundService播放音频文件
                    _soundService.PlayAudioFile(item.Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"播放音频失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
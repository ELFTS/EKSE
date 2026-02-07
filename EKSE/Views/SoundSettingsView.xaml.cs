using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using EKSE.Components;
using EKSE.Services;
using EKSE.Models;

namespace EKSE.Views
{
    public partial class SoundSettingsView : UserControl
    {
        private readonly ObservableCollection<AudioFileItem> _audioFilesList = new ObservableCollection<AudioFileItem>();
        
        private SoundService? _soundService;
        private ProfileManager? _profileManager;
        private AudioFileManager? _audioFileManager;
        
        private Key _selectedKey = Key.None;
        
        public SoundSettingsView()
        {
            InitializeComponent();
            AudioFilesListBox.ItemsSource = _audioFilesList;
            Unloaded += SoundSettingsView_Unloaded;
        }
        
        private void SoundSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
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
        private void VirtualKeyboardControl_KeySelected(object sender, VirtualKeyEventArgs e)
        {
            _selectedKey = e.Key;
            SelectedKeyText.Text = $"当前选中按键: {e.Key}";
            UpdateSoundPathDisplay();
        }

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
        
        public void SetServices(SoundService soundService, ProfileManager profileManager, AudioFileManager audioFileManager)
        {
            _soundService = soundService;
            _profileManager = profileManager;
            _audioFileManager = audioFileManager;
            
            if (_profileManager != null)
            {
                _profileManager.ProfilesChanged += OnProfilesChanged;
                _profileManager.CurrentProfileChanged += OnCurrentProfileChanged;
            }
            
            if (_audioFileManager != null)
            {
                _audioFileManager.AudioFilesChanged += OnAudioFilesChanged;
            }
            
            InitializeProfileUI();
            
            if (_soundService != null && _profileManager != null)
            {
                VirtualKeyboardControl.SetServices(_soundService, _profileManager);
            }
            
            RefreshAudioFilesList();
        }
        
        private void RefreshFullUI()
        {
            InitializeProfileUI();
            VirtualKeyboardControl.RefreshVisualState();
            RefreshAudioFilesList();
        }
        
        private void SafeRefreshFullUI()
        {
            if (Dispatcher.CheckAccess())
            {
                RefreshFullUI();
            }
            else
            {
                Dispatcher.Invoke(RefreshFullUI);
            }
        }
        
        private void OnProfilesChanged(object? sender, EventArgs e)
        {
            SafeRefreshFullUI();
        }
        
        private void OnCurrentProfileChanged(object? sender, EventArgs e)
        {
            SafeRefreshFullUI();
        }
        
        private void OnAudioFilesChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                RefreshAudioFilesList();
            }
            else
            {
                Dispatcher.Invoke(RefreshAudioFilesList);
            }
        }
        
        private void InitializeProfileUI()
        {
            if (_profileManager != null)
            {
                ProfileComboBox.ItemsSource = _profileManager.Profiles;
                ProfileComboBox.DisplayMemberPath = "Name";
                ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
                VirtualKeyboardControl.RefreshVisualState();
            }
        }
        
        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_profileManager != null && ProfileComboBox.SelectedItem is SoundProfile profile)
            {
                _profileManager.SwitchProfile(profile);
            }
        }
        
        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null) return;
            
            var newName = InputBox.Show("请输入方案名称:", "新建声音方案");
            
            if (!string.IsNullOrWhiteSpace(newName))
            {
                var newProfile = _profileManager.CreateProfile(newName.Trim());
                ProfileComboBox.SelectedItem = newProfile;
            }
        }
        
        private void RenameProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || _profileManager.CurrentProfile == null)
                return;

            var currentProfile = _profileManager.CurrentProfile;
            var newName = InputBox.Show("请输入新的方案名称:", "重命名声音方案", currentProfile.Name ?? "");
            
            if (!string.IsNullOrWhiteSpace(newName) && newName != currentProfile.Name)
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                if (newName.IndexOfAny(invalidChars) >= 0)
                {
                    MessageBox.Show("方案名称包含非法字符，请重新输入。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_profileManager.RenameProfile(currentProfile, newName))
                {
                    MessageBox.Show("重命名成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    _audioFileManager?.Refresh();
                    // SoundService.Refresh() 已移除，音频状态会自动处理
                    RefreshFullUI();
                }
                else
                {
                    MessageBox.Show("重命名失败，可能是由于权限不足或其他系统错误。请确保程序有足够的权限访问方案文件夹。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || ProfileComboBox.SelectedItem is not SoundProfile profile)
            {
                MessageBox.Show("请先选择一个要删除的声音方案。", "未选择方案", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (profile.Name == "默认方案")
            {
                MessageBox.Show("不能删除默认方案。", "操作不允许", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var result = MessageBox.Show(
                $"确定要删除声音方案 \"{profile.Name}\" 吗？此操作不可撤销。", 
                "确认删除", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _profileManager.DeleteProfile(profile);
                    RefreshFullUI();
                    MessageBox.Show("声音方案已成功删除。", "删除成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除方案时发生错误: {ex.Message}", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
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
                    RefreshFullUI();
                    ProfileComboBox.SelectedItem = importedProfile;
                    MessageBox.Show("声音方案导入成功！", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("声音方案导入失败！", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
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
            }
        }
        
        private void RefreshAudioFilesButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudioFilesList();
        }
        
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
                        Name = Path.GetFileName(file),
                        Path = file,
                        Size = sizeString
                    });
                }
            }
        }
        
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
        
        private void AudioFilesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (AudioFilesListBox.SelectedItem is AudioFileItem selectedItem && 
                _selectedKey != System.Windows.Input.Key.None &&
                _profileManager != null)
            {
                _profileManager.SetKeySound(_selectedKey, selectedItem.Path);
                UpdateSoundPathDisplay();
                VirtualKeyboardControl.RefreshVisualState();
                
                MessageBox.Show($"已将 \"{selectedItem.Name}\" 设置为按键 {_selectedKey} 的音效", 
                    "设置成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void RenameAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is AudioFileItem item &&
                _audioFileManager != null)
            {
                var newName = InputBox.Show("请输入新的文件名:", "重命名", item.Name);
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
        
        private void PlayAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is AudioFileItem item &&
                _soundService != null)
            {
                try
                {
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
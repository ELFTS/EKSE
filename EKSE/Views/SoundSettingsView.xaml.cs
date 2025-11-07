using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using EKSE.Models;
using EKSE.Services;
using EKSE.Components;
using System.Windows.Media;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
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
            public string Name { get; set; }
            public string Path { get; set; }
            public string Size { get; set; }
        }
        
        // 服务和管理器引用
        private SoundService _soundService;
        private ProfileManager _profileManager;
        private AudioFileManager _audioFileManager;
        private SoundProfile _currentProfile;
        
        // 平滑滚动相关字段
        private double _scrollOffset = 0;
        private ScrollViewer _scrollViewer; // 保存ScrollViewer引用
        private DispatcherTimer _scrollTimer;
        private double _targetOffset;
        private double _startOffset;
        private DateTime _scrollStartTime;
        private const int ScrollDuration = 300; // 滚动持续时间（毫秒）
        
        // 当前选中的按键
        private System.Windows.Input.Key _selectedKey = System.Windows.Input.Key.None;
        
        // 音频文件列表
        private ObservableCollection<AudioFileItem> _audioFilesList = new ObservableCollection<AudioFileItem>();

        public SoundSettingsView()
        {
            InitializeComponent();
            // 设置ListBox的数据源
            AudioFilesListBox.ItemsSource = _audioFilesList;
            
            // 在Loaded事件中获取ScrollViewer引用
            Loaded += (s, e) => {
                _scrollViewer = FindVisualChild<ScrollViewer>(this);
            };
        }

        // 当虚拟键盘上的按键被选中时
        private void VirtualKeyboardControl_KeySelected(object sender, VirtualKeyEventArgs e)
        {
            _selectedKey = e.Key;
            SelectedKeyText.Text = $"当前选中按键: {e.Key}";
            
            // 显示当前按键的音效路径
            UpdateSoundPathDisplay();
        }

        // 播放当前音效按钮点击事件
        private void PlaySoundButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKey != System.Windows.Input.Key.None && _soundService != null)
            {
                _soundService.PlaySound(_selectedKey);
            }
            else
            {
                MessageBox.Show("请先选择一个按键", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
            
            // 刷新音频文件列表
            RefreshAudioFiles();
        }
        
        // 初始化声音方案界面
        private void InitializeProfileUI()
        {
            if (_profileManager == null) return;
            
            // 绑定方案列表
            ProfileComboBox.ItemsSource = _profileManager.Profiles;
            ProfileComboBox.DisplayMemberPath = "Name";
            ProfileComboBox.SelectedItem = _profileManager.CurrentProfile;
        }
        
        // 方案选择变化事件
        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_profileManager == null || ProfileComboBox.SelectedItem == null) return;
            
            var selectedProfile = (SoundProfile)ProfileComboBox.SelectedItem;
            _profileManager.SetCurrentProfile(selectedProfile);
            
            // 更新按键音效路径显示
            UpdateSoundPathDisplay();
            
            // 强制刷新音频文件列表
            RefreshAudioFiles();
        }
        
        // 新建方案按钮点击事件
        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null) return;
            
            // 弹出输入框获取方案名称
            var profileName = Interaction.InputBox("请输入方案名称:", "新建方案", "新方案");
            if (!string.IsNullOrWhiteSpace(profileName))
            {
                try
                {
                    // 检查方案名称是否已存在
                    if (_profileManager.Profiles.Any(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("方案名称已存在，请使用其他名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // 创建新方案
                    var newProfile = _profileManager.CreateProfile(profileName);
                    
                    // 刷新界面
                    InitializeProfileUI();
                    
                    // 选中新创建的方案
                    ProfileComboBox.SelectedItem = newProfile;
                    
                    MessageBox.Show($"成功创建方案: {profileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 删除方案按钮点击事件
        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || ProfileComboBox.SelectedItem == null) return;
            
            var selectedProfile = (SoundProfile)ProfileComboBox.SelectedItem;
            
            // 至少保留一个方案
            if (_profileManager.Profiles.Count <= 1)
            {
                MessageBox.Show("至少需要保留一个声音方案", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var result = MessageBox.Show($"确定要删除方案 \"{selectedProfile.Name}\" 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _profileManager.DeleteProfile(selectedProfile);
                    
                    // 刷新界面
                    InitializeProfileUI();
                    
                    // 刷新音频文件列表
                    RefreshAudioFiles();
                    
                    MessageBox.Show("方案删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除方案失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 导出方案按钮点击事件
        private void ExportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null || ProfileComboBox.SelectedItem == null) return;
            
            var selectedProfile = (SoundProfile)ProfileComboBox.SelectedItem;
            
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "ZIP文件|*.zip",
                FileName = $"{selectedProfile.Name}.zip"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var success = _profileManager.ExportProfile(selectedProfile, saveFileDialog.FileName);
                    if (success)
                    {
                        MessageBox.Show("方案导出成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("方案导出失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"方案导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 导入方案按钮点击事件
        private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_profileManager == null) return;
            
            var openFileDialog = new OpenFileDialog
            {
                Title = "导入声音方案",
                Filter = "ZIP文件|*.zip"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var importedProfile = _profileManager.ImportProfile(openFileDialog.FileName);
                    if (importedProfile != null)
                    {
                        // 刷新界面
                        InitializeProfileUI();
                        
                        // 选中导入的方案
                        ProfileComboBox.SelectedItem = importedProfile;
                        
                        MessageBox.Show($"成功导入方案: {importedProfile.Name}", "导入成功", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("导入声音方案失败", "导入失败", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}", "导入失败", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        // 添加音频文件按钮点击事件
        private void AddAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "音频文件|*.wav;*.mp3;*.aac;*.wma;*.flac|所有文件|*.*",
                Multiselect = true
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    // 直接导入音频文件到当前方案的sounds文件夹，保持原始文件名
                    _profileManager?.ImportSoundToCurrentProfile(fileName);
                }
                
                // 刷新音频文件列表（从当前方案文件夹加载）
                RefreshAudioFiles();
            }
        }
        
        // 刷新音频文件列表
        private void RefreshAudioFiles()
        {
            if (_profileManager?.CurrentProfile == null) return;
            
            try
            {
                // 清空当前列表
                _audioFilesList.Clear();
                
                // 获取当前方案的sounds文件夹路径
                var keySoundsDirectory = Path.Combine(_profileManager.CurrentProfile.FilePath, "sounds");
                System.Diagnostics.Debug.WriteLine($"尝试加载目录: {keySoundsDirectory}");
                
                if (Directory.Exists(keySoundsDirectory))
                {
                    // 获取所有支持的音频文件
                    var supportedExtensions = new[] { ".wav", ".mp3", ".aac", ".wma", ".flac" };
                    var audioFiles = Directory.GetFiles(keySoundsDirectory, "*.*", SearchOption.AllDirectories)
                        .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        .ToList();
                    
                    // 更新列表
                    foreach (var file in audioFiles)
                    {
                        _audioFilesList.Add(new AudioFileItem
                        {
                            Name = Path.GetFileName(file),
                            Path = file,
                            Size = GetFileSize(file)
                        });
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"找到 {_audioFilesList.Count} 个音频文件");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"目录不存在: {keySoundsDirectory}");
                    // 确保目录存在
                    Directory.CreateDirectory(keySoundsDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新音频文件列表失败: {ex.Message}");
            }
        }
        
        // 获取文件大小
        private string GetFileSize(string filePath)
        {
            try
            {
                var info = new FileInfo(filePath);
                var size = info.Length;
                
                // 格式化文件大小
                if (size < 1024)
                    return $"{size} B";
                if (size < 1024 * 1024)
                    return $"{size / 1024.0:F1} KB";
                return $"{size / (1024.0 * 1024.0):F1} MB";
            }
            catch
            {
                return "未知";
            }
        }
        
        // 刷新音频文件按钮点击事件
        private void RefreshAudioFilesButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshAudioFiles();
        }
        
        // 音频文件列表双击事件
        private void AudioFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AudioFilesListBox.SelectedItem is AudioFileItem audioFileItem && _selectedKey != System.Windows.Input.Key.None)
            {
                // 检查按键是否已经设置了音效
                var currentSoundPath = _profileManager?.GetKeySound(_selectedKey);
                string message;
                
                if (!string.IsNullOrEmpty(currentSoundPath) && File.Exists(currentSoundPath))
                {
                    message = $"确定要将按键 {_selectedKey} 的音效从\n{System.IO.Path.GetFileName(currentSoundPath)}\n更换为\n{audioFileItem.Name}\n吗？";
                }
                else
                {
                    message = $"确定要将选中的音频文件设置为按键 {_selectedKey} 的音效吗？";
                }
                
                // 确认操作
                var result = MessageBox.Show(message, "确认设置", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 设置选中按键的音效
                    _profileManager?.SetKeySound(_selectedKey, audioFileItem.Path);
                    
                    // 更新显示
                    UpdateSoundPathDisplay();
                    
                    // 给用户提示
                    MessageBox.Show($"已将选中的音频文件设置为按键 {_selectedKey} 的音效", "设置成功", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        
        // 重命名音频文件按钮点击事件
        private void RenameAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AudioFileItem audioFileItem)
            {
                var newName = Microsoft.VisualBasic.Interaction.InputBox("请输入新的文件名:", "重命名文件", audioFileItem.Name);
                if (!string.IsNullOrWhiteSpace(newName) && newName != audioFileItem.Name)
                {
                    try
                    {
                        var newFileName = newName.Contains('.') ? newName : $"{newName}{Path.GetExtension(audioFileItem.Path)}";
                        var newFilePath = Path.Combine(Path.GetDirectoryName(audioFileItem.Path), newFileName);
                        
                        // 检查新文件名是否已存在
                        if (File.Exists(newFilePath))
                        {
                            var result = MessageBox.Show($"文件 {newFileName} 已存在，是否覆盖？", "确认覆盖", 
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result != MessageBoxResult.Yes)
                                return;
                        }
                        
                        // 重命名文件
                        File.Move(audioFileItem.Path, newFilePath);
                        
                        // 更新列表项
                        audioFileItem.Name = newFileName;
                        audioFileItem.Path = newFilePath;
                        
                        // 刷新界面
                        AudioFilesListBox.Items.Refresh();
                        
                        MessageBox.Show("文件重命名成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"重命名文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        // 删除音频文件按钮点击事件
        private void DeleteAudioFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AudioFileItem audioFileItem)
            {
                var result = MessageBox.Show($"确定要删除文件 {audioFileItem.Name} 吗？", "确认删除", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 删除文件
                        File.Delete(audioFileItem.Path);
                        
                        // 从列表中移除
                        _audioFilesList.Remove(audioFileItem);
                        
                        MessageBox.Show("文件删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        /// <summary>
        /// 平滑滚动事件处理
        /// </summary>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // 保存ScrollViewer引用
                _scrollViewer = scrollViewer;
                
                // 计算目标滚动偏移量
                double newOffset = scrollViewer.VerticalOffset - (e.Delta > 0 ? 50 : -50);
                
                // 限制滚动范围
                newOffset = Math.Max(0, newOffset);
                newOffset = Math.Min(scrollViewer.ScrollableHeight, newOffset);
                
                // 启动平滑滚动动画
                StartSmoothScroll(scrollViewer, newOffset);
                
                // 标记事件已处理，防止默认滚动行为
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// 启动平滑滚动动画
        /// </summary>
        private void StartSmoothScroll(ScrollViewer scrollViewer, double targetOffset)
        {
            // 初始化滚动动画参数
            _startOffset = scrollViewer.VerticalOffset;
            _targetOffset = targetOffset;
            _scrollStartTime = DateTime.Now;
            
            // 如果计时器尚未创建，则创建它
            if (_scrollTimer == null)
            {
                _scrollTimer = new DispatcherTimer();
                _scrollTimer.Interval = TimeSpan.FromMilliseconds(10); // 10毫秒更新一次
                _scrollTimer.Tick += ScrollTimer_Tick;
            }
            
            // 启动计时器
            _scrollTimer.Start();
        }
        
        /// <summary>
        /// 滚动计时器事件处理
        /// </summary>
        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            // 计算经过的时间
            double elapsed = (DateTime.Now - _scrollStartTime).TotalMilliseconds;
            
            // 计算进度（0到1之间）
            double progress = Math.Min(elapsed / ScrollDuration, 1.0);
            
            // 应用缓动函数（使用立方缓动）
            double easedProgress = 1 - Math.Pow(1 - progress, 3);
            
            // 计算当前偏移量
            double currentOffset = _startOffset + (_targetOffset - _startOffset) * easedProgress;
            
            // 设置滚动位置
            _scrollViewer?.ScrollToVerticalOffset(currentOffset);
            
            // 如果滚动完成，停止计时器
            if (progress >= 1.0)
            {
                _scrollTimer.Stop();
            }
        }
        
        /// <summary>
        /// 在可视化树中查找指定类型的子元素
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent) where T : class
        {
            if (parent == null) return default(T);
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var correctlyTyped = child as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                else
                {
                    var result = FindVisualChild<T>(child);
                    if (result != null)
                        return result;
                }
            }
            
            return default(T);
        }
    }
}
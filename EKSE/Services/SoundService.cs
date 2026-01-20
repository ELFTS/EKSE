using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;
using NAudio.Wave;
using System.Collections.Concurrent;

namespace EKSE.Services
{
    public class SoundService : IDisposable
    {
        private readonly WaveOutEvent _waveOut;
        private AudioFileReader? _audioFileReader;
        private ProfileManager _profileManager;
        
        // 音频资源池，用于缓存已经加载的音频文件信息（仅缓存文件路径和基本参数，不缓存实际的读取器）
        private readonly ConcurrentDictionary<string, (WaveFormat waveFormat, TimeSpan totalTimespan)> _audioInfoCache = new ConcurrentDictionary<string, (WaveFormat waveFormat, TimeSpan totalTimespan)>();
        
        // 最大缓存数量，防止内存过度使用
        private const int MaxCacheSize = 50;
        
        // 全局键盘钩子相关
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private AudioFileManager? _audioFileManager;
        
        // 事件定义
        public event EventHandler? SoundPlayed;
        public event EventHandler? SoundError;
        
        // 按键状态跟踪（使用ConcurrentDictionary提高并发性能）
        private readonly ConcurrentDictionary<Key, bool> _keyStates = new ConcurrentDictionary<Key, bool>();
        
        public SoundService(ProfileManager profileManager)
        {
            _profileManager = profileManager;
            _waveOut = new WaveOutEvent();
            
            // 订阅当前方案改变事件，以便在方案改变时释放相关资源
            _profileManager.CurrentProfileChanged += OnCurrentProfileChanged;
            
            // 设置全局键盘钩子
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }
        
        public SoundService(ProfileManager profileManager, AudioFileManager audioFileManager) : this(profileManager)
        {
            _audioFileManager = audioFileManager;
        }

        public void PlaySound(Key key)
        {
            try
            {
                // 检查是否启用音效
                if (Application.Current is App app && app.SettingsManager != null)
                {
                    var currentSettings = app.SettingsManager.GetCurrentSettings();
                    if (currentSettings != null && !currentSettings.EnableSound)
                    {
                        return;
                    }
                }
                
                // 获取按键对应的音效路径
                var soundPath = _profileManager?.GetKeySound(key);
                
                // 检查音效文件是否存在
                if (string.IsNullOrEmpty(soundPath) || !File.Exists(soundPath))
                {
                    SoundError?.Invoke(this, new SoundErrorEventArgs(key, "音效文件不存在"));
                    return;
                }
                
                // 播放音效
                PlayAudioInternal(soundPath);
                
                // 触发音效播放事件
                SoundPlayed?.Invoke(this, new SoundEventArgs(key, soundPath));
            }
            catch (Exception ex)
            {
                SoundError?.Invoke(this, new SoundErrorEventArgs(key, ex.Message));
            }
        }
        
        /// <summary>
        /// 内部方法：播放音频文件
        /// </summary>
        private void PlayAudioInternal(string audioFilePath)
        {
            try
            {
                // 使用lock确保线程安全
                lock (this)
                {
                    // 停止当前正在播放的音效
                    if (_waveOut.PlaybackState != PlaybackState.Stopped)
                    {
                        _waveOut.Stop();
                    }
                    
                    // 释放音频文件读取器资源
                    _audioFileReader?.Dispose();
                    _audioFileReader = null;
                    
                    // 创建新的音频文件读取器
                    _audioFileReader = new AudioFileReader(audioFilePath);
                    
                    // 缓存音频信息（仅缓存基本信息，不缓存读取器实例）
                    if (!_audioInfoCache.ContainsKey(audioFilePath) && _audioInfoCache.Count < MaxCacheSize)
                    {
                        _audioInfoCache.TryAdd(audioFilePath, (_audioFileReader.WaveFormat, _audioFileReader.TotalTime));
                    }
                    
                    // 重新初始化并播放
                    _waveOut.Init(_audioFileReader);
                    _waveOut.Play();
                }
            }
            catch
            {
                // 忽略异常，防止崩溃
            }
        }
        
        /// <summary>
        /// 重置音频服务状态
        /// </summary>
        private void ResetAudioState()
        {
            // 清空音频缓存
            _audioInfoCache.Clear();
            
            // 停止当前播放的音效
            try
            {
                _waveOut.Stop();
            }
            catch
            {
                // 忽略停止播放时的异常
            }
            
            // 释放音频文件读取器资源
            _audioFileReader?.Dispose();
            _audioFileReader = null;
        }
        
        /// <summary>
        /// 当前方案改变时的处理方法
        /// </summary>
        private void OnCurrentProfileChanged(object? sender, EventArgs e)
        {
            ResetAudioState();
        }
        
        /// <summary>
        /// 刷新服务状态以匹配当前配置方案
        /// </summary>
        public void Refresh()
        {
            ResetAudioState();
        }
        
        // 全局键盘钩子实现
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule?.ModuleName ?? string.Empty), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    // 检查当前按键状态
                    bool currentState;
                    if (!_keyStates.TryGetValue(key, out currentState) || !currentState)
                    {
                        // 按键未被按下，播放音效并更新状态
                        PlaySound(key);
                        _keyStates[key] = true;
                    }
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    // 更新按键状态为已释放
                    _keyStates[key] = false;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region Windows API 导入
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        public void Dispose()
        {
            // 取消钩子
            UnhookWindowsHookEx(_hookID);
            
            // 清空音频缓存
            _audioInfoCache.Clear();
            
            // 释放音频设备
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFileReader?.Dispose();
            
            // 清理按键状态集合
            _keyStates.Clear();
        }
    /// <summary>
    /// 直接播放指定路径的音频文件
    /// </summary>
    /// <param name="audioFilePath">音频文件路径</param>
    public void PlayAudioFile(string audioFilePath)
    {
        try
        {
            // 检查音频文件是否存在
            if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
            {
                return;
            }
            
            // 播放音频
            PlayAudioInternal(audioFilePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"播放音频时出错: {ex.Message}");
        }
    }
    
    
    /// <summary>
    /// 声音事件参数
    /// </summary>
    public class SoundEventArgs : EventArgs
    {
        public Key Key { get; }
        public string SoundPath { get; }
        
        public SoundEventArgs(Key key, string soundPath)
        {
            Key = key;
            SoundPath = soundPath;
        }
    }
    
    /// <summary>
    /// 声音错误事件参数
    /// </summary>
    public class SoundErrorEventArgs : EventArgs
    {
        public Key Key { get; }
        public string ErrorMessage { get; }
        
        public SoundErrorEventArgs(Key key, string errorMessage)
        {
            Key = key;
            ErrorMessage = errorMessage;
        }
    }
}
}
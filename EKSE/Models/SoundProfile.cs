using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace EKSE.Models
{
    /// <summary>
    /// 声音方案模型
    /// </summary>
    public class SoundProfile
    {
        /// <summary>
        /// 方案名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        /// <summary>
        /// 模式（例如：随机）
        /// </summary>
        [JsonPropertyName("mode")]
        public string Mode { get; set; }
        
        /// <summary>
        /// 重复音效文件
        /// </summary>
        [JsonPropertyName("repeat_sound")]
        public string RepeatSound { get; set; }
        
        /// <summary>
        /// 分配的声音列表
        /// </summary>
        [JsonPropertyName("assigned_sounds")]
        public List<SoundAssignment> AssignedSounds { get; set; }
        
        /// <summary>
        /// 按键到音效文件路径的映射（用于运行时）
        /// </summary>
        [JsonIgnore]
        public Dictionary<Key, string> KeySounds { get; set; }
        
        /// <summary>
        /// 默认音效文件路径
        /// </summary>
        [JsonIgnore]
        public string DefaultSound { get; set; }
        
        /// <summary>
        /// 方案文件路径
        /// </summary>
        [JsonIgnore]
        public string FilePath { get; set; }
        
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public SoundProfile()
        {
            KeySounds = new Dictionary<Key, string>();
            AssignedSounds = new List<SoundAssignment>(); // 确保总是初始化
        }
        
        /// <summary>
        /// 带名称的构造函数
        /// </summary>
        /// <param name="name">方案名称</param>
        public SoundProfile(string name) : this()
        {
            Name = name;
        }
    }
}
using System.Text.Json.Serialization;

namespace EKSE.Models
{
    /// <summary>
    /// 声音分配模型，用于序列化/反序列化JSON中的assigned_sounds数组项
    /// </summary>
    public class SoundAssignment
    {
        /// <summary>
        /// 按键名称
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; }
        
        /// <summary>
        /// 音效文件名
        /// </summary>
        [JsonPropertyName("sound")]
        public string Sound { get; set; }
    }
}
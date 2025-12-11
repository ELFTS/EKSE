using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EKSE.Models
{
    /// <summary>
    /// 自定义的SoundProfile JSON转换器，用于忽略不需要的字段
    /// </summary>
    public class SoundProfileJsonConverter : JsonConverter<SoundProfile>
    {
        public override SoundProfile ReadJson(JsonReader reader, Type objectType, SoundProfile existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            
            var profile = new SoundProfile();
            
            // 读取标准字段
            profile.Name = jo["name"]?.ToString();
            profile.Mode = jo["mode"]?.ToString();
            profile.RepeatSound = jo["repeat_sound"]?.ToString();
            
            // 读取assigned_sounds数组
            var assignedSoundsToken = jo["assigned_sounds"];
            if (assignedSoundsToken != null && assignedSoundsToken.Type == JTokenType.Array)
            {
                profile.AssignedSounds = new List<SoundAssignment>(); // 确保初始化列表
                foreach (var item in assignedSoundsToken)
                {
                    var key = item["key"]?.ToString();
                    var sound = item["sound"]?.ToString();
                    
                    if (key != null && sound != null)
                    {
                        profile.AssignedSounds.Add(new SoundAssignment
                        {
                            Key = key,
                            Sound = sound
                        });
                    }
                }
            }
            else
            {
                // 即使没有assigned_sounds属性，也要确保AssignedSounds被初始化
                profile.AssignedSounds = new List<SoundAssignment>();
            }
            
            // 忽略single_key和soundsList字段
            // 这些字段不会被处理或存储到profile对象中
            
            return profile;
        }

        public override void WriteJson(JsonWriter writer, SoundProfile value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // 写入标准字段
            writer.WritePropertyName("name");
            writer.WriteValue(value.Name ?? string.Empty);
            
            writer.WritePropertyName("mode");
            writer.WriteValue(value.Mode ?? string.Empty);
            
            writer.WritePropertyName("repeat_sound");
            writer.WriteValue(value.RepeatSound ?? string.Empty);
            
            // 写入assigned_sounds数组
            writer.WritePropertyName("assigned_sounds");
            writer.WriteStartArray();
            if (value.AssignedSounds != null)
            {
                foreach (var assignment in value.AssignedSounds)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("key");
                    writer.WriteValue(assignment.Key ?? string.Empty);
                    writer.WritePropertyName("sound");
                    writer.WriteValue(assignment.Sound ?? string.Empty);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            
            writer.WriteEndObject();
        }
    }
}
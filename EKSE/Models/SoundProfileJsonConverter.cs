using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EKSE.Models
{
    /// <summary>
    /// 自定义的SoundProfile JSON转换器，用于忽略不需要的字段
    /// </summary>
    public class SoundProfileJsonConverter : JsonConverter<SoundProfile>
    {
        public override SoundProfile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            
            var profile = new SoundProfile();
            
            // 读取标准字段
            if (root.TryGetProperty("name", out var nameElement))
            {
                profile.Name = nameElement.GetString();
            }
            
            if (root.TryGetProperty("mode", out var modeElement))
            {
                profile.Mode = modeElement.GetString();
            }
            
            if (root.TryGetProperty("repeat_sound", out var repeatSoundElement))
            {
                profile.RepeatSound = repeatSoundElement.GetString();
            }
            
            // 读取assigned_sounds数组
            if (root.TryGetProperty("assigned_sounds", out var assignedSoundsElement) && assignedSoundsElement.ValueKind == JsonValueKind.Array)
            {
                profile.AssignedSounds = new List<SoundAssignment>(); // 确保初始化列表
                foreach (var item in assignedSoundsElement.EnumerateArray())
                {
                    if (item.TryGetProperty("key", out var keyElement) && item.TryGetProperty("sound", out var soundElement))
                    {
                        profile.AssignedSounds.Add(new SoundAssignment
                        {
                            Key = keyElement.GetString(),
                            Sound = soundElement.GetString()
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

        public override void Write(Utf8JsonWriter writer, SoundProfile value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            
            // 写入标准字段
            writer.WriteString("name", value.Name);
            writer.WriteString("mode", value.Mode);
            writer.WriteString("repeat_sound", value.RepeatSound);
            
            // 写入assigned_sounds数组
            writer.WritePropertyName("assigned_sounds");
            writer.WriteStartArray();
            if (value.AssignedSounds != null)
            {
                foreach (var assignment in value.AssignedSounds)
                {
                    writer.WriteStartObject();
                    writer.WriteString("key", assignment.Key);
                    writer.WriteString("sound", assignment.Sound);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            
            writer.WriteEndObject();
        }
    }
}
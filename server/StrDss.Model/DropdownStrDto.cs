﻿using System.Text.Json.Serialization;

namespace StrDss.Model
{
    public class DropdownStrDto
    {
        [JsonPropertyName("value")]
        public string Id { get; set; } = "";
        [JsonPropertyName("label")]
        public string Description { get; set; } = "";
    }
}

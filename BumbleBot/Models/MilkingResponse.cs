﻿using Newtonsoft.Json;

namespace BumbleBot.Models
{
    public class MilkingResponse
    {
        [JsonProperty("message")] public string Message { get; set; }
    }
}
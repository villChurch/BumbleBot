using System;
using Newtonsoft.Json;

namespace BumbleBot.Models
{
    public class DailyResponse
    {
        [JsonProperty("response")]
        public string message { get; set; }
    }
}

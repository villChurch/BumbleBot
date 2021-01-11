using Newtonsoft.Json;

namespace BumbleBot.Models
{
    public class DailyResponse
    {
        [JsonProperty("response")] public string Message { get; set; }
    }
}
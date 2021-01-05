using Newtonsoft.Json;

namespace BumbleBot.Models
{
    public class ExpiryResponse
    {
        [JsonProperty("id")] public int id { get; set; }

        [JsonProperty("discordID")] public ulong discordID { get; set; }

        [JsonProperty("milk")] public decimal milk { get; set; }

        [JsonProperty("expirydate")] public string expiryDate { get; set; }
    }
}
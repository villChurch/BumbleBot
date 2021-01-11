using Newtonsoft.Json;

namespace BumbleBot.Models
{
    public class ExpiryResponse
    {
        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("discordID")] public ulong DiscordId { get; set; }

        [JsonProperty("milk")] public decimal Milk { get; set; }

        [JsonProperty("expirydate")] public string ExpiryDate { get; set; }
    }
}
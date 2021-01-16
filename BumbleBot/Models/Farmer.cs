namespace BumbleBot.Models
{
    public class Farmer
    {
        public ulong DiscordId { get; set; }
        public int Credits { get; set; }
        public int Barnspace { get; set; }
        public int Grazingspace { get; set; }
        public decimal Milk { get; set; }
    }
}
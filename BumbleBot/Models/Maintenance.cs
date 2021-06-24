namespace BumbleBot.Models
{
    public class Maintenance
    {
        public int id { get; set; }
        
        public ulong farmerId { get; set; }
        
        public bool needsMaintenance { get; set; }
        
        public bool milkingBoost { get; set; }
        
        public bool dailyBoost { get; set; }
    }
}
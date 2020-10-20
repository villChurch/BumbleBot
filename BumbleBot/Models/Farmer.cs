using System;
namespace BumbleBot.Models
{
    public class Farmer
    {
        public ulong discordID { get; set; }
        public int credits { get; set; }
        public int barnspace { get; set; }
        public int grazingspace { get; set; }
        public Farmer()
        {
        }
    }
}

using System;
namespace BumbleBot.Models
{
    public class Farmer
    {
        private ulong discordID { get; set; }
        private Barn barn { get; set; }
        private int credits { get; set; }
        public Farmer()
        {
        }
    }
}

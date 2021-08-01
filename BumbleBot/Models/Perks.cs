namespace BumbleBot.Models
{
    public class Perks
    {
        public int id { get; set; }
        public string perkName { get; set; }
        public string perkBonusText { get; set; }
        public int perkCost { get; set; }

        public int levelUnlocked { get; set; }
        
        public int requires { get; set; }
    }
}
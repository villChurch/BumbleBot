using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models;

[Table("guesswhoLeaderboard")]
public class GuessWhoLeaderboard
{
    
    [Column]
    public int id { get; set; }
    
    [Column]
    public string discordID { get; set; }
}
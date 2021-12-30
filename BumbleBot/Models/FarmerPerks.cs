using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models;

[Table("farmerperks")]
public class FarmerPerks
{
    [Column("id")]
    public int id { get; set; }
    
    [Column("farmerid")]
    public ulong farmerId { get; set; }
    
    [Column("perkid")]
    public int perkId { get; set; }
}
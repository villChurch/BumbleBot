using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models;

[Table("grazing")]
public class Grazing
{
    [Column("goatId")]
    public int goatId { get; set; }
    
    [Column("farmerId")]
    public ulong farmerId { get; set; }
}
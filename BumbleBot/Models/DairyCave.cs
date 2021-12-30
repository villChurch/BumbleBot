using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models;

[Table("dairycave")]
public class DairyCave
{
    [Column("ownerID")]
    public ulong ownerID { get; set; }

    [Column("softCheese")]
    private decimal softCheese { get; set; }
    
    [Column("slots")]
    private int slots { get; set; }
}
using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models;

[Table("specialgoats")]
public class SpecialGoats
{
    [Column("id")]
    public int id { get; set; }
    [Column("variation")]
    public string variation { get; set; }
    [Column("KidFileLink")]
    public string kidFileLink { get; set; }
    [Column("AdultFileLink")]
    public string adultFileLink { get; set; }
    [Column("enabled")]
    public bool enabled { get; set; }
}
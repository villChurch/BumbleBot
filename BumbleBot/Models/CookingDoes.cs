using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models;

[Table("cookingdoes")]
public class CookingDoes
{
    [Column("id")]
    public int id { get; set; }
    
    [Column("goatId")]
    public int goatId { get; set; }
    
    [Column("dueDate")]
    public DateTime date { get; set; }
    
    [Column("ready")]
    public bool ready { get; set; }
}
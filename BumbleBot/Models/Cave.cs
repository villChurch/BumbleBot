using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models
{
    [Table("dairycave")]
    public class Cave
    {
        [Column("ownerID")]
        [Key]
        public string ownerId { get; set; }

        [Column("softCheese")]
        public decimal SoftCheese { get; set; }

        [Column("slots")]
        public int Slots { get; set; }
    }
}
using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models
{
    [Table("dairy")]
    public class Dairy
    {
        [Column("id")]
        public int id { get; set; }
        [Column("ownerID")]
        public ulong ownerID { get; set; }
        [Column("milk")]
        public decimal milk { get; set; }
        [Column("slots")]
        public int slots { get; set; }
        [Column("softcheese")]
        public decimal softcheese { get; set; }
        [Column("hardcheese")]
        public decimal hardcheese { get; set; }
    }
}
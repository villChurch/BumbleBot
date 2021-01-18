using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models
{
    [Table("farmers")]
    public class Farmer
    {
        [Column("DiscordID")]
        [Key]
        public ulong DiscordId { get; set; }

        [Column("credits")]
        public int Credits { get; set; }

        [Column("barnsize")]
        public int Barnspace { get; set; }

        [Column("grazesize")]
        public int Grazingspace { get; set; }

        [Column("milk")]
        public decimal Milk { get; set; }

        [Column("oats")]
        public int oats { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BumbleBot.Models
{
    [Table("info")]
    public class Info
    {
        [Column("name")]
        [Key]
        public string Name { get; set; }

        [Column("value")]
        public string Value { get; set; }
    }
}


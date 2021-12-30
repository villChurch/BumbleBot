using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
// ReSharper disable InconsistentNaming

namespace BumbleBot.Models
{
    public enum Type
    {
        Adult,
        Kid
    }

    public enum Breed
    {
        Nubian,
        Nigerian_Dwarf,
        La_Mancha,
        Christmas,
        Minx,
        Bumble,
        Zenyatta,
        Tailless,
        Valentines,
        Shamrock,
        Spring,
        Dazzle,
        MemberSpecial,
        DairySpecial,
        SummerSpecial,
        Buck,
        BotAnniversarySpecial,
        Juliet,
        Percy,
        Seven,
        Halloween,
        November
    }

    public enum BaseColour
    {
        White,
        Black,
        Red,
        Chocolate,
        Gold,
        Special
    }

    [Table("goats")]
    public class Goat
    {
        [Column("level")]
        public int Level { get; set; }
        
        [Column("name")]
        public string Name { get; set; }
        
        [Column("levelMultiplier")]
        public decimal LevelMulitplier { get; set; }
        
        [Column("id")]
        public int Id { get; set; }
        
        [Column("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Type Type { get; set; }

        [Column("breed")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Breed Breed { get; set; }

        [Column("baseColour")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BaseColour BaseColour { get; set; }

        [Column("equipped")]
        public bool Equipped { get; set; }
        
        [Column("experience")]
        public decimal Experience { get; set; }
        
        [Column("imageLink")]
        public string FilePath { get; set; }

        [Column("ownerID")]
        public ulong ownerId { get; set; }
    }
}
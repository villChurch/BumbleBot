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
        Tailless
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

    public class Goat
    {
        public bool Special { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public decimal LevelMulitplier { get; set; }
        public int Id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Type Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Breed Breed { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BaseColour BaseColour { get; set; }

        public bool Equiped { get; set; }
        public decimal Experience { get; set; }
        public string FilePath { get; set; }
    }
}
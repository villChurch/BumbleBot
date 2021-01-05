using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        Zenyatta
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
        public bool special { get; set; }
        public int level { get; set; }
        public string name { get; set; }
        public decimal levelMulitplier { get; set; }
        public int id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Type type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Breed breed { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public BaseColour baseColour { get; set; }

        public bool equiped { get; set; }
        public decimal experience { get; set; }
        public string filePath { get; set; }
    }
}
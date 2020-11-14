using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
namespace Bumblebot.Models
{

    public partial class TriviaQuestions
    {
        [JsonProperty("questions")]
        public Question[] Questions { get; set; }
    }

    public partial class Question
    {
        [JsonProperty("question")]
        public string QuestionQuestion { get; set; }

        [JsonProperty("difficulty")]
        public Difficulty Difficulty { get; set; }

        [JsonProperty("correct_answer")]
        public string CorrectAnswer { get; set; }

        [JsonProperty("incorrect_answers")]
        public IncorrectAnswer[] IncorrectAnswers { get; set; }
    }

    public enum Difficulty { Easy, Medium };

    public partial struct IncorrectAnswer
    {
        public bool? Bool;
        public string String;

        public static implicit operator IncorrectAnswer(bool Bool) => new IncorrectAnswer { Bool = Bool };
        public static implicit operator IncorrectAnswer(string String) => new IncorrectAnswer { String = String };
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                DifficultyConverter.Singleton,
                IncorrectAnswerConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class DifficultyConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Difficulty) || t == typeof(Difficulty?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "easy":
                    return Difficulty.Easy;
                case "medium":
                    return Difficulty.Medium;
            }
            throw new Exception("Cannot unmarshal type Difficulty");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Difficulty)untypedValue;
            switch (value)
            {
                case Difficulty.Easy:
                    serializer.Serialize(writer, "easy");
                    return;
                case Difficulty.Medium:
                    serializer.Serialize(writer, "medium");
                    return;
            }
            throw new Exception("Cannot marshal type Difficulty");
        }

        public static readonly DifficultyConverter Singleton = new DifficultyConverter();
    }

    internal class IncorrectAnswerConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(IncorrectAnswer) || t == typeof(IncorrectAnswer?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    var boolValue = serializer.Deserialize<bool>(reader);
                    return new IncorrectAnswer { Bool = boolValue };
                case JsonToken.String:
                case JsonToken.Date:
                    var stringValue = serializer.Deserialize<string>(reader);
                    return new IncorrectAnswer { String = stringValue };
            }
            throw new Exception("Cannot unmarshal type IncorrectAnswer");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            var value = (IncorrectAnswer)untypedValue;
            if (value.Bool != null)
            {
                serializer.Serialize(writer, value.Bool.Value);
                return;
            }
            if (value.String != null)
            {
                serializer.Serialize(writer, value.String);
                return;
            }
            throw new Exception("Cannot marshal type IncorrectAnswer");
        }

        public static readonly IncorrectAnswerConverter Singleton = new IncorrectAnswerConverter();
    }
}

using System.Threading.Tasks;
using BumbleBot.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace BumbleBot.Converter
{
    public class ReminderTimeConverter : IArgumentConverter<ReminderTime.TimeValue>
    {
        public Task<Optional<ReminderTime.TimeValue>> ConvertAsync(string value, CommandContext ctx)
        {
            switch (value.ToLower())
            {
                case "hour" or "hours":
                    return Task.FromResult(Optional.FromValue(ReminderTime.TimeValue.Hour));
                case "minute" or "minutes":
                    return Task.FromResult(Optional.FromValue(ReminderTime.TimeValue.Minute));
                case "second" or "seconds":
                    return Task.FromResult(Optional.FromValue(ReminderTime.TimeValue.Second));
                case "day" or "days":
                    return Task.FromResult(Optional.FromValue(ReminderTime.TimeValue.Day));
                case "month" or "months":
                    return Task.FromResult(Optional.FromValue(ReminderTime.TimeValue.Month));
                case "year" or "years":
                    return Task.FromResult(Optional.FromValue(ReminderTime.TimeValue.Year));
                default:
                    return Task.FromResult(Optional.FromNoValue<ReminderTime.TimeValue>());
            }
        }
    }
}
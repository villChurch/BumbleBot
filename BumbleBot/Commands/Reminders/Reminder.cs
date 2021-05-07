using System;
using System.Globalization;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;

namespace BumbleBot.Commands.Reminders
{
    [Group("reminder")]
    [Aliases("remind", "remindme")]
    public class Reminder : BaseCommandModule
    {
        private ReminderService reminderService { get; }
        public Reminder(ReminderService reminderService)
        {
            this.reminderService = reminderService;
        }

        [Command("set")]
        [Description("Create a reminder")]
        public async Task CreateReminder(CommandContext ctx, [Description("When the reminder is to be sent"), RemainingText] string dataToParse)
        {
            await ctx.TriggerTypingAsync();

            var (duration, text) = Dates.ParseTime(dataToParse);
            
            if (string.IsNullOrWhiteSpace(text) || text.Length > 128)
            {
                await ctx.Channel.SendMessageAsync(
                    "Reminder text must to be no longer than 128 characters, not empty and not whitespace.");
                return;
            }
#if !DEBUG
            if (duration < TimeSpan.FromSeconds(30))
            {
                await ctx.ElevatedRespondAsync("Minimum required time span to set a reminder is 30 seconds.");
                return;
            }
#endif

            if (duration > TimeSpan.FromDays(365)) // 1 year is the maximum
            {
                await ctx.Channel.SendMessageAsync("Maximum allowed time span to set a reminder is 1 year.");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var dispatchAt = now + duration;

            await reminderService.AddReminderToDataBase(dispatchAt, ctx.Member, BreakMentions(text), ctx.Message);
            var emoji = DiscordEmoji.FromName(ctx.Client, ":alarm_clock:");
            await ctx.Channel.SendMessageAsync(
                $"{emoji} Ok, in {duration.Humanize(4, minUnit: TimeUnit.Second)} " +
                $"I will remind you about the following:\n\n{BreakMentions(text)}").ConfigureAwait(false);
        }

        private static string BreakMentions(string input)
        {
            input = input.Replace("@", "@\u200B");
            return input;
        }
    }
}
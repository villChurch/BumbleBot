using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.MiscCommands
{
    public class Misc : BaseCommandModule
    {

        [Command("roll")]
        [Aliases("random")]
        [Description("Roll a random number between 0 and the number you enter")]
        public async Task RandomRoll(CommandContext ctx, [Description("Max value")] int max)
        {
            if (max <= 0)
            {
                await ctx.Channel.SendMessageAsync("Number must be greater than 0").ConfigureAwait(false);
            }
            else
            {
                Random rnd = new Random();
                var number = rnd.Next(0, max);
                await new DiscordMessageBuilder()
                    .WithContent($"{number}")
                    .WithReply(ctx.Message.Id, true)
                    .SendAsync(ctx.Channel)
                    .ConfigureAwait(false);
            }
        }
    }
}
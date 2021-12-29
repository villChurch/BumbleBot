using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;

namespace BumbleBot.Commands;

[SlashCommandGroup("dairy", "dairy commands")]
public class DairySlashCommands : ApplicationCommandsModule
{
    [SlashCommandGroup("cheese", "Commands to do with cheese")]
    public class DairyCheeseCommands : ApplicationCommandsModule
    {
        [SlashCommand("add", "adds soft cheese to your cave")]
        public async Task AddSoftCheeseToCave(InteractionContext ctx,
            [Option("amount", "amount of cheese to add")] int amount)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"Imagine {amount} of cheese has been added as will hasn't coded this to test")
                    .AsEphemeral(true));
        }
    }

    [SlashCommandGroup("milk", "Commands to do with milk")]
    public class DairyMilkCommands : ApplicationCommandsModule
    {
        [SlashCommand("add", "adds milk to dairy")]
        public async Task AddMilkToDairy(InteractionContext ctx, [Option("amount", "amount of milk to add")] int amount)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"Imagine {amount} of milk has been added as will hasn't coded this to test")
                    .AsEphemeral(true));
        }
    }
}
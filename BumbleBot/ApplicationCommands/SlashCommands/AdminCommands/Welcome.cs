
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Utilities;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;

namespace BumbleBot.ApplicationCommands.SlashCommands.AdminCommands
{
    public class Welcome : ApplicationCommandsModule
    {
        private ModalHelper modalHelper = new();
        private WelcomeUtilities welcomeUtilities = new();

        [SlashCommand("welcome_set", "Sets welcome message for this server")]
        [OwnerOrPermissionSlash(DisCatSharp.Permissions.KickMembers)]
        public async Task SetWelcomeMessage(InteractionContext ctx, [Option("channel", "channel to post welcome message in")] DiscordChannel discordChannel)
        {
            var id = modalHelper.RandomID(8);
            var textId = modalHelper.RandomID(8);

            var modal = await ctx.Interaction.CreatePaginatedModalResponseAsync(
                new List<ModalPage>()
                {
                    new ModalPage(new DiscordInteractionModalBuilder()
                    .WithCustomId(id)
                    .WithTitle("Set welcome message for this server")
                    .AddModalComponents(new DiscordTextComponent(DisCatSharp.Enums.TextComponentStyle.Paragraph,
                    textId, "Message")))
                });

            if (modal.TimedOut)
            {
                await modal.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Interaction has timed out"));
            }
            else
            {
                var message = modal.Responses[textId];
                await welcomeUtilities.InsertOrUpdateWelcomeMessage(ctx.Guild, discordChannel.Id.ToString(), message);
                await modal.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Welcome message updated"));
            }
        }

        [SlashCommand("welcome_test", "tests the welcome message")]
        [OwnerOrPermissionSlash(DisCatSharp.Permissions.KickMembers)]
        public async Task TestWelcomeMessage(InteractionContext ctx, [Option("channel", "channel to post test message in")] DiscordChannel discordChannel,
            [Option("user", "user to use for test message")] DiscordUser discordUser)
        {
            await ctx.CreateResponseAsync(DisCatSharp.InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AsEphemeral(false));
            var message = welcomeUtilities.ReturnCompletedWelcomeMessage(discordChannel.Guild, discordUser);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent(message));
        }

        [SlashCommand("welcome_delete", "deletes the welcome message for this guild")]
        [OwnerOrPermissionSlash(DisCatSharp.Permissions.KickMembers)]
        public async Task DeleteWelcomeMessage(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(DisCatSharp.InteractionResponseType.DeferredChannelMessageWithSource);
            await welcomeUtilities.DeleteWelcomeMessage(ctx.Guild);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent($"Deleted Welcome message for {ctx.Guild.Name}"));
        }
    }
}


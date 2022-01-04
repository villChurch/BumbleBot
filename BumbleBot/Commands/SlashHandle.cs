using System;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.ApplicationCommands;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands
{
    public class SlashHandle : ApplicationCommandsModule
    {
        private GoatService goatService = new GoatService();
        private FarmerService farmerService = new FarmerService();
        private DairyService dairyService = new DairyService();
        private DbUtils dbUtils = new DbUtils();

        [SlashCommand("clear_invalid_guild_slash", "Clears invalid slash commands in the current guild")]
        [OwnerOrPermissionSlash(Permissions.KickMembers)]
        public async Task ClearInvalidGuildSlashAsync(InteractionContext ctx)
        {
            if (ctx.User.Id != 272151652344266762)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not allowed to run this command.")
                        .AsEphemeral(true));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Running cleanup..."));
                var cmds = ctx.ApplicationCommandsExtension.RegisteredCommands.Where(rc => rc.Key == ctx.Guild.Id);
                var dcmds = await ctx.Client.GetGuildApplicationCommandsAsync(ctx.Guild.Id);
                foreach (var dcmd in dcmds)
                {
                    var keyValuePairs = cmds.ToList();
                    if (!keyValuePairs.Any(c =>
                            Enumerable.Where<DiscordApplicationCommand>(c.Value, sc => sc.Id == dcmd.Id).Any()))
                    {
                        await ctx.Client.DeleteGuildApplicationCommandAsync(ctx.Guild.Id, dcmd.Id);
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"Deleted command `{dcmd.Name} with ID `{dcmd.Id}` due to invalid state.`"));
                    }
                    else
                    {
                        await ctx.FollowUpAsync(
                            new DiscordFollowupMessageBuilder().WithContent(
                                $"Keeping command `{dcmd.Name} with ID `{dcmd.Id}`.`"));
                    }
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Done."));
            }
        }

        [SlashCommand("clear_invalid_slash", "Clears invalid slash commands")]
        [OwnerOrPermissionSlash(Permissions.KickMembers)]
        public async Task ClearInvalidSlashAsync(InteractionContext ctx)
        {
            if (ctx.User.Id != 272151652344266762)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("You are not allowed to run this command.")
                        .AsEphemeral(true));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Running cleanup..."));
                var cmds = ctx.ApplicationCommandsExtension.RegisteredCommands.Where(rc => rc.Key == null);
                var dcmds = await ctx.Client.GetGlobalApplicationCommandsAsync();
                foreach (var dcmd in dcmds)
                {
                    if (!cmds.Where(c => c.Value.Where(sc => sc.Id == dcmd.Id).Any()).Any())
                    {
                        await ctx.Client.DeleteGlobalApplicationCommandAsync(dcmd.Id);
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"Deleted command `{dcmd.Name} with ID `{dcmd.Id}` due to invalid state.`"));
                    }
                    else
                    {
                        await ctx.FollowUpAsync(
                            new DiscordFollowupMessageBuilder().WithContent(
                                $"Keeping command `{dcmd.Name} with ID `{dcmd.Id}`.`"));
                    }
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Done."));
            }
        }
        
        [SlashCommand("ping", "Ping pong")]
        [OwnerOrPermissionSlash(Permissions.KickMembers)]
        public async Task SlashPing(InteractionContext ctx)
        {
            DiscordInteractionResponseBuilder discordInteractionResponseBuilder = new();
            discordInteractionResponseBuilder.Content = $"Pong! Webhook latency is {ctx.Client.Ping}ms";
            discordInteractionResponseBuilder.IsEphemeral = true;
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                discordInteractionResponseBuilder);
        }
        
        [IsUserAvailableSlash]
        [SlashCommand("milk", "Display how much milk you have")]
        public async Task DisplayMilk(InteractionContext ctx)
        {
            var milk = farmerService.ReturnFarmerInfo(ctx.User.Id).Milk;
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"You currently have {milk} lbs of milk.")
                        .AsEphemeral(true))
                .ConfigureAwait(false);
        }

        [IsUserAvailableSlash]
        [SlashCommand("cheese", "Display how much soft or hard cheese you have")]
        public async Task DisplayCheese(InteractionContext ctx, [Choice("soft_cheese", "soft")]
            [Choice("hard_cheese", "hard")] [Option("type_of_cheese", "Type of cheese")] String cheese)
        {
            var dairy = dairyService.GetUsersDairy(ctx.User.Id);
            if (cheese.Equals("hard"))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            $"You currently have {dairy.HardCheese} lbs of hard cheese.")
                            .AsEphemeral(true))
                    .ConfigureAwait(false);
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            $"You currently have {dairy.SoftCheese} lbs of soft cheese.")
                            .AsEphemeral(true))
                    .ConfigureAwait(false);
            }
        }
        
        //[IsUserAvailableSlash]
        [SlashCommand("credits", "Display your balance")]
        public async Task DisplayBalance(InteractionContext ctx)
        {
            var balance = farmerService.ReturnFarmerInfo(ctx.User.Id).Credits;
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"Current balance is {balance:n0}").AsEphemeral(true)).ConfigureAwait(false);
        }

        [IsUserAvailableSlash]
        [SlashCommand("handle", "Handles a goat")]
        public async Task HandleGoat(InteractionContext ctx, [Option("goat_to_handle", "Id of goat to handle")]
            int goatId)
        {
            var gId = goatId;
            var goats = goatService.ReturnUsersGoats(ctx.User.Id);
            if (goats.Count < 1)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You do not have any goats.")
                        .AsEphemeral(true)).ConfigureAwait(false);
            }
            else if (goats.Find(g => g.Id == gId) != null)
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Update goats set equipped = 0 where ownerID = ?ownerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Update goats set equipped = 1 where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.Int32).Value = gId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Goat is now in hand.")
                        .AsEphemeral(true))
                    .ConfigureAwait(false);
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Could not find a goat with id {goatId}.")
                            .AsEphemeral(true))
                    .ConfigureAwait(false);
            }
        }
    }
}
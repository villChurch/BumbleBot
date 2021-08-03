using System;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands
{
    public class SlashHandle : SlashCommandModule
    {
        private GoatService goatService = new GoatService();
        private FarmerService farmerService = new FarmerService();
        private DairyService dairyService = new DairyService();
        private DbUtils dbUtils = new DbUtils();

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
                            $"You currently have {dairy.HardCheese} lbs of hard cheese."))
                    .ConfigureAwait(false);
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            $"You currently have {dairy.SoftCheese} lbs of soft cheese."))
                    .ConfigureAwait(false);
            }
        }
        
        [IsUserAvailableSlash]
        [SlashCommand("credits", "Display your balance")]
        public async Task DisplayBalance(InteractionContext ctx)
        {
            var balance = farmerService.ReturnFarmerInfo(ctx.User.Id).Credits;
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"Current balance is {balance:n0}")).ConfigureAwait(false);
        }

        [IsUserAvailableSlash]
        [SlashCommand("handle", "Handles a goat")]
        public async Task HandleGoat(InteractionContext ctx, [Option("goat_to_handle", "Id of goat to handle")]
            long goatId)
        {
            var gId = (int) goatId;
            var goats = goatService.ReturnUsersGoats(ctx.User.Id);
            if (goats.Count < 1)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You do not have any goats.")).ConfigureAwait(false);
            }
            else if (goats.Find(g => g.Id == gId) != null)
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Update goats set equipped = 0 where ownerID = ?ownerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Update goats set equipped = 1 where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.Int32).Value = gId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Goat is now in hand.")).ConfigureAwait(false);
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"Could not find a goat with id {goatId}."))
                    .ConfigureAwait(false);
            }
        }
    }
}
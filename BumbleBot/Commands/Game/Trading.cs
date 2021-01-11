using System;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class Trading : BaseCommandModule
    {
        private readonly DBUtils dBUtils = new DBUtils();

        public Trading(FarmerService farmerService, GoatService goatService)
        {
            this.farmerService = farmerService;
            this.goatService = goatService;
        }

        private FarmerService farmerService { get; }
        private GoatService goatService { get; }

        [Command("trade")]
        [Aliases("gift", "give")]
        public async Task TradeGoat(CommandContext ctx, [Description("goat id you want to trade/gift")]
            int goatId,
            [Description("member you want to trade/gift the goat to")]
            DiscordMember recipient)
        {
            var recipientFarmer = farmerService.ReturnFarmerInfo(recipient.Id);
            var sendersGoats = goatService.ReturnUsersGoats(ctx.User.Id);
            if (sendersGoats.Select(x => x.id == goatId).ToList().Count < 1)
            {
                await ctx.Channel.SendMessageAsync($"You do not own a goat with id {goatId}").ConfigureAwait(false);
            }
            else if (recipientFarmer.barnspace < 10)
            {
                await ctx.Channel.SendMessageAsync($"{recipient.Mention} does not have a profile setup.")
                    .ConfigureAwait(false);
            }
            else if (!goatService.CanGoatsFitInBarn(recipient.Id, 1))
            {
                await ctx.Channel
                    .SendMessageAsync($"{recipient.Mention} does not have enough room in their barn for this goat")
                    .ConfigureAwait(false);
            }
            else if (goatService.IsGoatCooking(goatId))
            {
                await ctx.Channel
                    .SendMessageAsync($"Goat with id {goatId} is currently in your shelter and cannot be moved")
                    .ConfigureAwait(false);
            }
            else
            {
                var goat = sendersGoats.First(x => x.id == goatId);
                _ = TradeGoat(ctx, recipient, goat);
            }
        }

        private async Task TradeGoat(CommandContext ctx, DiscordMember recipient, Goat goat)
        {
            try
            {
                var url = "http://williamspires.com/";
                var interactivity = ctx.Client.GetInteractivity();
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Incoming gift from {ctx.User.Mention}",
                    ImageUrl = url + goat.filePath.Replace(" ", "%20")
                };
                embed.AddField("Name", goat.name);
                embed.AddField("Level", goat.level.ToString(), true);
                embed.AddField("Experience", goat.experience.ToString(), true);
                embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                var message = await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                //white_check_mark
                var yesEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                var noEmoji = DiscordEmoji.FromName(ctx.Client, ":x:");
                await message.CreateReactionAsync(yesEmoji).ConfigureAwait(false);
                await message.CreateReactionAsync(noEmoji).ConfigureAwait(false);
                var result = await interactivity.WaitForReactionAsync(x => x.Message == message &&
                                                                           x.User.Id == recipient.Id
                                                                           && (x.Emoji == yesEmoji ||
                                                                               x.Emoji == noEmoji),
                    TimeSpan.FromMinutes(2)).ConfigureAwait(false);

                if (result.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Goat trade has timed out").ConfigureAwait(false);
                }
                else if (result.Result.Emoji == yesEmoji)
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Update goats Set ownerID = ?recipientId where id = ?goatId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?recipientId", MySqlDbType.VarChar).Value = recipient.Id;
                        command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Delete from grazing where goatId = ?goatId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel
                        .SendMessageAsync($"Goat {goat.name} has now been given to {recipient.DisplayName}")
                        .ConfigureAwait(false);
                }
                else if (result.Result.Emoji == noEmoji)
                {
                    await ctx.Channel.SendMessageAsync($"{recipient.DisplayName} has rejected the trade")
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
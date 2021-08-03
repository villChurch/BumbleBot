using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [IsUserAvailable]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class Trading : BaseCommandModule
    {
        private readonly DbUtils dBUtils = new();
        private readonly PerkService perkService;

        public Trading(FarmerService farmerService, GoatService goatService, PerkService perkService)
        {
            this.FarmerService = farmerService;
            this.GoatService = goatService;
            this.perkService = perkService;
        }

        private FarmerService FarmerService { get; }
        private GoatService GoatService { get; }

        [Command("trade")]
        [Aliases("gift", "give")]
        public async Task TradeGoat(CommandContext ctx, [Description("goat id you want to trade/gift")]
            int goatId,
            [Description("member you want to trade/gift the goat to")]
            DiscordMember recipient)
        {
            var recipientFarmer = FarmerService.ReturnFarmerInfo(recipient.Id);
            var sendersGoats = GoatService.ReturnUsersGoats(ctx.User.Id);
            var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
            if (sendersGoats.Select(x => x.Id == goatId).ToList().Count < 1)
            {
                await ctx.Channel.SendMessageAsync($"You do not own a goat with id {goatId}").ConfigureAwait(false);
            }
            else if (recipientFarmer.Barnspace < 10)
            {
                await ctx.Channel.SendMessageAsync($"{recipient.Mention} does not have a profile setup.")
                    .ConfigureAwait(false);
            }
            else if (!GoatService.CanGoatsFitInBarn(recipient.Id, 1, usersPerks))
            {
                await ctx.Channel
                    .SendMessageAsync($"{recipient.Mention} does not have enough room in their barn for this goat")
                    .ConfigureAwait(false);
            }
            else if (GoatService.IsGoatCooking(goatId))
            {
                await ctx.Channel
                    .SendMessageAsync($"Goat with id {goatId} is currently in your shelter and cannot be moved")
                    .ConfigureAwait(false);
            }
            else
            {
                var goat = sendersGoats.First(x => x.Id == goatId);
                _ = TradeGoat(ctx, recipient, goat);
            }
        }

        private async Task TradeGoat(CommandContext ctx, DiscordMember recipient, Goat goat)
        {
            try
            {
                var url = "https://williamspires.com/";
                var interactivity = ctx.Client.GetInteractivity();
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Incoming gift from {ctx.User.Mention}",
                    ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                };
                embed.AddField("Name", goat.Name);
                embed.AddField("Level", goat.Level.ToString(), true);
                embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
                embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
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
                        var query = "Update goats Set ownerID = ?recipientId where id = ?goatId and equipped = 0";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?recipientId", MySqlDbType.VarChar).Value = recipient.Id;
                        command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Delete from grazing where goatId = ?goatId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel
                        .SendMessageAsync($"Goat {goat.Name} has now been given to {recipient.DisplayName}")
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
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Command?.QualifiedName ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }
    }
}
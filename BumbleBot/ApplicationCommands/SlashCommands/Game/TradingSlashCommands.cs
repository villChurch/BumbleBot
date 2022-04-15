using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BumbleBot.ApplicationCommands.SlashCommands.Game;

public class TradingSlashCommands : ApplicationCommandsModule
{
    private readonly DbUtils dbUtils = new();
    private readonly PerkService perkService;
    private readonly FarmerService farmerService;
    private readonly GoatService goatService;

    public TradingSlashCommands(GoatService goatService, FarmerService farmerService, PerkService perkService)
    {
        this.perkService = perkService;
        this.farmerService = farmerService;
        this.goatService = goatService;
    }

    [SlashCommand("trade", "trade a goat to another player")]
    [IsUserAvailableSlash]
    public async Task TradeGoat(InteractionContext ctx, [Option("goatId", "id of the goat you want to trade")] int goatId,
        [Option("recipient", "member you want to trade the goat with")] DiscordUser recipient)
    {
        var recipientFarmer = farmerService.ReturnFarmerInfo(recipient.Id);
        var sendersGoats = goatService.ReturnUsersGoats(ctx.User.Id);
        var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
        if (sendersGoats.Select(x => x.Id == goatId).ToList().Count < 1)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"You do not own a goat with id {goatId}"));
        }
        else if (recipientFarmer.Barnspace < 10)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"{recipient.Mention} does not have a profile setup."));
        }
        else if (!goatService.CanGoatsFitInBarn(recipient.Id, 1, usersPerks, ctx.Client.Logger))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"{recipient.Mention} does not have enough room in their barn for this goat"));
        }
        else if (goatService.IsGoatCooking(goatId))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"Goat with id {goatId} is currently in your shelter and cannot be moved"));
        }
        else
        {
            var goat = sendersGoats.First(x => x.Id == goatId);
            _ = TradeGoat(ctx, recipient, goat);
        }
    }

    private async Task TradeGoat(InteractionContext ctx, DiscordUser recipient, Goat goat)
    {
        try
        {
            var url = "https://williamspires.com/";
            var interactivity = ctx.Client.GetInteractivity();
            var embed = new DiscordEmbedBuilder
            {
                Title = $"Incoming gift from {ctx.User.Username}",
                ImageUrl = url + goat.FilePath.Replace(" ", "%20")
            };
            embed.AddFields(new List<DiscordEmbedField>()
            {
                new("Name", goat.Name),
                new("Level", goat.Level.ToString(), true),
                new("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true),
                new("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true),
                new("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true)
            });
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed));
            var message = await ctx.Interaction.GetOriginalResponseAsync();
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
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Goat trade has timed out"));
            }
            else if (result.Result.Emoji == yesEmoji)
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Update goats Set ownerID = ?recipientId where id = ?goatId and equipped = 0";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?recipientId", MySqlDbType.VarChar).Value = recipient.Id;
                    command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Delete from grazing where goatId = ?goatId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                //TODO goats don't appear to get traded
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"Goat {goat.Name} has now been given to {recipient.Username}"));
            }
            else if (result.Result.Emoji == noEmoji)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"{recipient.Username} has rejected the trade"));
            }
        }
        catch (Exception ex)
        {
            ctx.Client.Logger.Log(LogLevel.Error,
                "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                ex.GetType(), ex.Message);
        }
    }
}
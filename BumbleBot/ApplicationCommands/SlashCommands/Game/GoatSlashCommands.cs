using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.ApplicationCommands.SlashCommands.Game;

[SlashCommandGroup("goat", "General goat commands")]
public class GoatSlashCommands : ApplicationCommandsModule
{
    private readonly DbUtils dBUtils = new();
    private readonly FarmerService farmerService;
    private readonly GoatService goatService;

    public GoatSlashCommands(FarmerService farmerService, GoatService goatService)
    {
        this.farmerService = farmerService;
        this.goatService = goatService;
    }

    [SlashCommand("prefix", "Give all your goats a prefix")]
    [IsUserAvailableSlash]
    public async Task PrefixGoats(InteractionContext ctx, [Option("_prefix", "prefix to add")] string herdName)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        _ = Task.Run(async () =>
            await SendAndPostResponse(ctx, $"http://localhost:8080/goat/herd/rename/{ctx.User.Id}/{herdName}"));
    }

    [SlashCommand("rprefix", "Remove a prefix from all of your goats")]
    [IsUserAvailableSlash]
    public async Task RemovePrefixFromGoats(InteractionContext ctx,
        [Option("_prefix", "prefix to remove")] string prefix)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        _ = Task.Run(async () =>
            await SendAndPostResponse(ctx, $"http://localhost:8080/goat/herd/prefix/remove/{ctx.User.Id}/{prefix}"));
    }

    private static async Task SendAndPostResponse(InteractionContext ctx, string url)
    {
        // send and post response from api here
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        using (var response = (HttpWebResponse)await request.GetResponseAsync())
        using (var stream = response.GetResponseStream())
        {
            using (var reader = new StreamReader(stream))
            {
                var stringResponse = await reader.ReadToEndAsync();

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent(stringResponse));
            }
        }
    }

    [SlashCommand("refresh", "Update goat images and type for goats that haven't grown")]
    [IsUserAvailableSlash]
    public async Task RefreshGoats(InteractionContext ctx)
    {
        goatService.UpdateGoatImagesForKidsThatAreAdults(ctx.User.Id);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent("Goats have been updated"));
    }

    [SlashCommand("stats", "show statistics relating to goats you own")]
    [IsUserAvailableSlash]
    public async Task ShowGoatStatistics(InteractionContext ctx)
    {
        var goats = goatService.ReturnUsersGoats(ctx.User.Id);
        var deadGoats = goatService.ReturnUsersDeadGoats(ctx.User.Id);
        var kidsInPen = goatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);

        var embed = new DiscordEmbedBuilder()
        {
            Title = $"{((DiscordMember)ctx.User).DisplayName}'s Goat statistics",
            Color = DiscordColor.Aquamarine
        };
        embed.AddField("Number of goats owned", goats.Count.ToString(), true);
        embed.AddField("Number of adults", goats.FindAll(goat => goat.Type == Type.Adult).Count.ToString(), true);
        embed.AddField("Number of kids", goats.FindAll(goat => goat.Type == Type.Kid).Count.ToString(), true);
        embed.AddField("Number of Nubian goats", goats.FindAll(goat => goat.Breed == Breed.Nubian).Count.ToString(),
            true);
        embed.AddField("Number of La Mancha goats",
            goats.FindAll(goat => goat.Breed == Breed.La_Mancha).Count.ToString(), true);
        embed.AddField("Number of Nigerian Dwarf goats",
            goats.FindAll(goat => goat.Breed == Breed.Nigerian_Dwarf).Count.ToString(), true);
        embed.AddField("Number of Special goats",
            goats.FindAll(goat => goat.BaseColour == BaseColour.Special).Count.ToString(), true);
        embed.AddField("Number of Chocolate goats",
            goats.FindAll(goat => goat.BaseColour == BaseColour.Chocolate).Count.ToString(), true);
        embed.AddField("Number of Black goats",
            goats.FindAll(goat => goat.BaseColour == BaseColour.Black).Count.ToString(), true);
        embed.AddField("Number of White goats",
            goats.FindAll(goat => goat.BaseColour == BaseColour.White).Count.ToString(), true);
        embed.AddField("Number of Gold goats",
            goats.FindAll(goat => goat.BaseColour == BaseColour.Gold).Count.ToString(), true);
        embed.AddField("Number of Red goats",
            goats.FindAll(goat => goat.BaseColour == BaseColour.Red).Count.ToString(), true);
        embed.AddField("Number of kids in shelter", kidsInPen.Count.ToString(), true);
        embed.AddField("Number of goats in memorial", deadGoats.Count.ToString(), true);
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed));
    }

    public enum OrderedChoices
    {
        [ChoiceName("level")] Level,
        [ChoiceName("breed")] Breed,
        [ChoiceName("colour")] Colour
    }

    [SlashCommand("ordered", "Shows your goats ordered by a parameter")]
    [IsUserAvailableSlash]
    public async Task ShowGoatsOrdered(InteractionContext ctx,
        [Option("orderBy", "how to order your goats")] OrderedChoices orderChoice)
    {
        try
        {
            switch (orderChoice)
            {
                case OrderedChoices.Level:
                {
                    var goats = goatService.ReturnUsersGoats(ctx.User.Id).OrderByDescending(x => x.Level);
                    var url = "https://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in goats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.Id}",
                            ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.Name);
                        embed.AddField("Level", goat.Level.ToString(), true);
                        embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
                        embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                        embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }

                    _ = Task.Run(async () => await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false,
                            ctx.User,
                            pages, null,
                            PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable,
                            CancellationToken.None)
                        .ConfigureAwait(false));
                    break;
                }
                case OrderedChoices.Breed:
                {
                    var goats = goatService.ReturnUsersGoats(ctx.User.Id).OrderBy(x => x.Breed);
                    var url = "https://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in goats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.Id}",
                            ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.Name);
                        embed.AddField("Level", goat.Level.ToString(), true);
                        embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
                        embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                        embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }

                    _ = Task.Run(async () => await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false,
                            ctx.User,
                            pages, null,
                            PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable,
                            CancellationToken.None)
                        .ConfigureAwait(false));
                    break;
                }
                case OrderedChoices.Colour:
                {
                    var goats = goatService.ReturnUsersGoats(ctx.User.Id).OrderBy(x => x.BaseColour);
                    var url = "https://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in goats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.Id}",
                            ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.Name);
                        embed.AddField("Level", goat.Level.ToString(), true);
                        embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
                        embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                        embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }

                    _ = Task.Run(async () => await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false,
                            ctx.User,
                            pages, null,
                            PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable,
                            CancellationToken.None)
                        .ConfigureAwait(false));
                    break;
                }
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

    [SlashCommand("show", "show your goats")]
    [IsUserAvailableSlash]
    public async Task ShowGoats(InteractionContext ctx)
    {
        try
        {
            var goats = goatService.ReturnUsersGoats(ctx.User.Id);

            var url = "https://williamspires.com/";
            var pages = new List<Page>();
            var interactivity = ctx.Client.GetInteractivity();
            foreach (var goat in goats)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{goat.Id}",
                    ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                };
                embed.AddField("Name", goat.Name);
                embed.AddField("Level", goat.Level.ToString(), true);
                embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
                embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                var page = new Page
                {
                    Embed = embed
                };
                pages.Add(page);
            }

            _ = Task.Run(async () => await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages,
                    null,
                    PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable, CancellationToken.None)
                .ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            ctx.Client.Logger.Log(LogLevel.Error,
                "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                ex.GetType(), ex.Message);
        }
    }

    [SlashCommand("rename", "renames a goat")]
    [IsUserAvailableSlash]
    public async Task RenameGoat(InteractionContext ctx, [Option("goatId", "id of goat to rename")] int goatId,
        [Option("new_name", "New name for your goat")] string newName)
    {
        try
        {
            var goats = goatService.ReturnUsersGoats(ctx.User.Id);
            if (goats.Any(x => x.Id == goatId))
            {
                var goat = goats.Where(x => x.Id == goatId && x.Breed == Breed.Dazzle);
                var rnd = new Random();
                var randomNum = rnd.Next(3);
                if (goat.Any() && randomNum != 1)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent("Unfortunately Dazzle follows her own rules and refuses to be renamed at the moment. Try again later."));
                }
                else
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                    {
                        var query = "Update goats Set name = ?newName where id = ?goatId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?newName", MySqlDbType.VarChar).Value = newName;
                        command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goatId;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    var changedGoat = goats.Where(x => x.Id == goatId);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"{changedGoat.First().Name} has been renamed to {newName}"));
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You do not own a goat with this ID"));
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

    [SlashCommand("sell", "sell a goat")]
    [IsUserAvailableSlash]
    public async Task SellGoat(InteractionContext ctx, [Option ("goat_id", "id of goat to sell")] int goatId)
    {
        try
        {
            var goats = goatService.ReturnUsersGoats(ctx.User.Id);
            if (goats.Any(goat => goat.Id == goatId))
            {
                goatService.DeleteGoat(goatId);
                var goat = goats.First(g => g.Id == goatId);
                var creditsToAdd = goat.Type == Type.Adult
                    ? (int)Math.Ceiling(goat.Level * 1.35)
                    : (int)Math.Ceiling(goat.Level * 0.75);
                var loanString = "";
                if (farmerService.DoesFarmerHaveALoan(ctx.User.Id))
                {
                    var (repaymentAmount, loanAmount) =
                        farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, creditsToAdd);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                    farmerService.AddCreditsToFarmer(ctx.User.Id, (creditsToAdd - repaymentAmount));
                }
                else
                {
                    farmerService.AddCreditsToFarmer(ctx.User.Id, creditsToAdd);
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"You have sold {goat.Name} to market for {creditsToAdd:n0} " +
                    $"credits. {loanString}"));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent($"You do not own a goat with id {goatId}."));
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

    [SlashCommand("memorial", "See all your past goats")]
    [IsUserAvailableSlash]
    public async Task SeeDeadGoats(InteractionContext ctx)
    {
        try
        {
            var goats = goatService.ReturnUsersDeadGoats(ctx.User.Id);

            var url = "https://williamspires.com/";
            var pages = new List<Page>();
            var interactivity = ctx.Client.GetInteractivity();
            foreach (var goat in goats)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{goat.Id}",
                    ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                };
                embed.AddField("Name", goat.Name);
                embed.AddField("Level", goat.Level.ToString(), true);
                embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
                embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                var page = new Page
                {
                    Embed = embed
                };
                pages.Add(page);
            }

            _ = Task.Run(async () => await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages,
                    null,
                    PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable, CancellationToken.None)
                .ConfigureAwait(false));
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
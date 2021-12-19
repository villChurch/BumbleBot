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
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace BumbleBot.Commands.Game
{
    [Group("breeding")]
    [Aliases("breed")]
    [IsUserAvailable]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class BreedingCommands : BaseCommandModule
    {
  
        public BreedingCommands(FarmerService farmerService, GoatService goatService)
        {
            FarmerService = farmerService;
            GoatService = goatService;
        }

        private FarmerService FarmerService { get; }
        private GoatService GoatService { get; }

        [Command("show")]
        [Description("Show current goats that have been sent to breed")]
        public async Task ShowGoatsInKiddingPenToBreed(CommandContext ctx)
        {
            try
            {
                if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!FarmerService.DoesFarmerHaveAdultsInKiddingPen(GoatService.ReturnUsersGoats(ctx.User.Id)))
                {
                    await ctx.Channel.SendMessageAsync("You currently do not have any adult goats in your kidding pen")
                        .ConfigureAwait(false);
                }
                else
                {
                    var (breedingIds, dictionary) = GoatService.ReturnUsersAdultGoatIdsInKiddingPen(ctx.User.Id);
                    var breedingGoats =
                        GoatService.ReturnUsersGoats(ctx.User.Id).Where(goat => breedingIds.Contains(goat.Id)).ToList();
                    const string url = "http://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in breedingGoats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.Id}",
                            ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.Name);
                        embed.AddField("Due Date", dictionary[goat.Id]);
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

                    _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, null,
                        PaginationBehaviour.WrapAround,ButtonPaginationBehavior.Disable,CancellationToken.None).ConfigureAwait(false));
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

        [Command("add")]
        [Description("Move a goat to the shelter")]
        public async Task MoveGoatToKiddingPen(CommandContext ctx, [Description("id of goat to move")] int goatId)
        {
            try
            {
                if (FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    var goats = GoatService.ReturnUsersGoats(ctx.User.Id);
                    var goatsBreeding = GoatService.ReturnUsersAdultGoatIdsInKiddingPen(ctx.User.Id);

                    if (goats.Where(goat => goat.Id == goatId).ToList().Count == 0)
                    {
                        await ctx.Channel.SendMessageAsync($"It appears you don't own a goat with id {goatId}")
                            .ConfigureAwait(false);
                    }
                    else if (goats.Find(goat => goat.Id == goatId)?.Level < 100)
                    {
                        await ctx.Channel
                            .SendMessageAsync(
                                "This goat is not yet an adult and only adults can be moved to the shelter")
                            .ConfigureAwait(false);
                    }
                    else if (goats.Find(goat => goat.Id == goatId)?.BaseColour == BaseColour.Special)
                    {
                        await ctx.Channel.SendMessageAsync("You cannot breed special goats").ConfigureAwait(false);
                    }
                    else if (goatsBreeding.Item2.ContainsKey(goatId))
                    {
                        await ctx.Channel.SendMessageAsync("This goat is already in the shelter").ConfigureAwait(false);
                    }
                    else
                    {
                        var uri = $"http://localhost:8080/breeding/{ctx.User.Id}/{goatId}";
                        var request = (HttpWebRequest) WebRequest.Create(uri);
                        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                        using var response = (HttpWebResponse) await request.GetResponseAsync();
                        await using (var stream = response.GetResponseStream())
                        using (var reader = new StreamReader(stream))
                        {
                            var stringResponse = await reader.ReadToEndAsync();

                            await ctx.Channel.SendMessageAsync(stringResponse).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("You need to purchase the shelter/kidding pen first")
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
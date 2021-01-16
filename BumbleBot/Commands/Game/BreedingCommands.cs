using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace BumbleBot.Commands.Game
{
    [Group("breeding")]
    [Aliases("breed")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class BreedingCommands : BaseCommandModule
    {
        private DbUtils dBUtils = new DbUtils();

        public BreedingCommands(FarmerService farmerService, GoatService goatService)
        {
            this.FarmerService = farmerService;
            this.GoatService = goatService;
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
                    var result = GoatService.ReturnUsersAdultGoatIdsInKiddingPen(ctx.User.Id);
                    var breedingIds = result.Item1; //GoatService.ReturnUsersAdultGoatIdsInKiddingPen(ctx.User.Id);
                    var breedingGoats =
                        GoatService.ReturnUsersGoats(ctx.User.Id).Where(goat => breedingIds.Contains(goat.Id)).ToList();
                    var url = "http://williamspires.com/";
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
                        embed.AddField("Due Date", result.Item2[goat.Id], false);
                        embed.AddField("Level", goat.Level.ToString(), true);
                        embed.AddField("Experience", goat.Experience.ToString(), true);
                        embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed).Replace("_", " "), true);
                        embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }

                    _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("add")]
        [Description("Move a goat to the kidding pen")]
        public async Task MoveGoatToKiddingPen(CommandContext ctx, [Description("id of goat to move")] int goatId)
        {
            try
            {
                if (FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    var goats = GoatService.ReturnUsersGoats(ctx.User.Id);

                    if (goats.Where(goat => goat.Id == goatId).ToList().Count == 0)
                    {
                        await ctx.Channel.SendMessageAsync($"It appears you don't own a goat with id {goatId}")
                            .ConfigureAwait(false);
                    }
                    else if (goats.Find(goat => goat.Id == goatId).Level < 100)
                    {
                        await ctx.Channel
                            .SendMessageAsync(
                                "This goat is not yet an adult and only adults can be moved to the shelter")
                            .ConfigureAwait(false);
                    }
                    else if (goats.Find(goat => goat.Id == goatId).BaseColour == BaseColour.Special)
                    {
                        await ctx.Channel.SendMessageAsync("You cannot breed special goats").ConfigureAwait(false);
                    }
                    else
                    {
                        // /breeding/{id}/{goatId}
                        var uri = $"http://localhost:8080/breeding/{ctx.User.Id}/{goatId}";
                        var request = (HttpWebRequest) WebRequest.Create(uri);
                        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                        using (var response = (HttpWebResponse) await request.GetResponseAsync())
                        using (var stream = response.GetResponseStream())
                        using (var reader = new StreamReader(stream))
                        {
                            var stringResponse = reader.ReadToEnd();

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
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
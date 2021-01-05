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
        private DBUtils dBUtils = new DBUtils();

        public BreedingCommands(FarmerService farmerService, GoatService goatService)
        {
            this.farmerService = farmerService;
            this.goatService = goatService;
        }

        private FarmerService farmerService { get; }
        private GoatService goatService { get; }

        [Command("show")]
        [Description("Show current goats that have been sent to breed")]
        public async Task ShowGoatsInKiddingPenToBreed(CommandContext ctx)
        {
            try
            {
                if (!farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!farmerService.DoesFarmerHaveAdultsInKiddingPen(goatService.ReturnUsersGoats(ctx.User.Id)))
                {
                    await ctx.Channel.SendMessageAsync("You currently do not have any adult goats in your kidding pen")
                        .ConfigureAwait(false);
                }
                else
                {
                    var breedingIds = goatService.ReturnUsersAdultGoatIdsInKiddingPen(ctx.User.Id);
                    var breedingGoats =
                        goatService.ReturnUsersGoats(ctx.User.Id).Where(goat => breedingIds.Contains(goat.id)).ToList();
                    var url = "http://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in breedingGoats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.id}",
                            ImageUrl = url + goat.filePath.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.name);
                        embed.AddField("Level", goat.level.ToString(), true);
                        embed.AddField("Experience", goat.experience.ToString(), true);
                        embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                        embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }

                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false);
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
                if (farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    var goats = goatService.ReturnUsersGoats(ctx.User.Id);

                    if (goats.Where(goat => goat.id == goatId).ToList().Count == 0)
                    {
                        await ctx.Channel.SendMessageAsync($"It appears you don't own a goat with id {goatId}")
                            .ConfigureAwait(false);
                    }
                    else if (goats.Find(goat => goat.id == goatId).level < 100)
                    {
                        await ctx.Channel
                            .SendMessageAsync(
                                "This goat is not yet an adult and only adults can be moved to the shelter")
                            .ConfigureAwait(false);
                    }
                    else if (goats.Find(goat => goat.id == goatId).baseColour == BaseColour.Special)
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
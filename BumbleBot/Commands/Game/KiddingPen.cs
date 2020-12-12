using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace BumbleBot.Commands.Game
{
    [Group("shelter")]
    [Description("Shelter commands")]
    public class KiddingPen : BaseCommandModule
    {
        FarmerService farmerService { get; }
        GoatService goatService { get; }

        public KiddingPen(FarmerService farmerService, GoatService goatService)
        {
            this.farmerService = farmerService;
            this.goatService = goatService;
        }

        [Command("move")]
        [Aliases("transfer")]
        [Description("move a kid into your goat pen")]
        public async Task MoveKidIntoKiddingPen(CommandContext ctx, [Description("id of kid to move")] int id)
        {
            try
            {
                if (!farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!farmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any kids in your kidding pen").ConfigureAwait(false);
                }
                else
                {
                    List<Goat> kids = goatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    var kidToMove = kids.Find(x => x.id == id);
                    if (String.IsNullOrEmpty(kidToMove.name))
                    {
                        await ctx.Channel.SendMessageAsync("You don't have a kid in your shelter with this id").ConfigureAwait(false);
                    }
                    else if (goatService.CanGoatFitInBarn(ctx.User.Id))
                    {
                        goatService.MoveKidIntoGoatPen(kidToMove, ctx.User.Id);
                        await ctx.Channel.SendMessageAsync($"Kid with id {id} has been moved into your barn").ConfigureAwait(false);
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync("There is no room in your barn for this kid at the moment").ConfigureAwait(false);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("sell")]
        [Description("Sell a kid in your shelter")]
        public async Task SellKidInKiddingPen(CommandContext ctx, [Description("id of kid to sell")]int id)
        {
            try
            {
                if (!farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!farmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any kids in your kidding pen").ConfigureAwait(false);
                }
                else
                {
                    List<Goat> kids = goatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    var kidToSell = kids.Find(x => x.id == id);
                    if (String.IsNullOrEmpty(kidToSell.name))
                    {
                        await ctx.Channel.SendMessageAsync("You don't have a kid in your shelter with this id").ConfigureAwait(false);
                    }
                    else
                    {
                        goatService.DeleteKidFromKiddingPen(id);
                        farmerService.AddCreditsToFarmer(ctx.User.Id, kidToSell.level * 2);
                        await ctx.Channel.SendMessageAsync($"You have sold kid with id {id} for {kidToSell.level * 2} credits").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("Shows the kids in your shelter")]
        public async Task ShowKidsInKiddingPen(CommandContext ctx)
        {
            try
            {
                if (!farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!farmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any kids in your kidding pen").ConfigureAwait(false);
                }
                else
                {
                    List<Goat> kids = goatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    var url = "http://williamspires.com/";
                    List<Page> pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in kids)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.id}",
                            ImageUrl = url + goat.filePath.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.name, false);
                        embed.AddField("Level", goat.level.ToString(), true);
                        embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                        embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                        Page page = new Page
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
    }
}

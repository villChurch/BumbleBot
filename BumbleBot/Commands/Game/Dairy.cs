using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BumbleBot.Commands.Game
{
    [Group("dairy")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class Dairy : BaseCommandModule
    {
        private DairyService dairyService { get; }
        private FarmerService farmerService { get; }
        public Dairy(DairyService dairyService, FarmerService farmerService)
        {
            this.dairyService = dairyService;
            this.farmerService = farmerService;
        }

        [Command("sell")]
        [Description("Sell the contents of your dairy")]
        public async Task SellContentsOfDairy(CommandContext ctx)
        {
            if (!dairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else
            {
                var dairy = dairyService.GetUsersDairy(ctx.User.Id);
                if (dairy.softCheese <= 0 && dairy.hardCheese <= 0)
                {
                    await ctx.Channel.SendMessageAsync("You don't have anything in your dairy that can be sold.").ConfigureAwait(false);
                }
                else
                {
                    int sellAmount = 0;
                    sellAmount += (int)Math.Ceiling(dairy.softCheese * 120);
                    farmerService.AddCreditsToFarmer(ctx.User.Id, sellAmount);
                    dairyService.RemoveSoftCheeseFromPlayer(ctx.User.Id, null);
                    await ctx.Channel.SendMessageAsync($"Sold contents of your dairy for {sellAmount}").ConfigureAwait(false);
                }
            }
        }

        [Command("add")]
        [Description("add milk to the dairy to produce cheese")]
        public async Task AddMilkToDairy(CommandContext ctx, int milk)
        {
            if (milk % 10 != 0)
            {
                await ctx.Channel.SendMessageAsync("Milk has to be added to the dairy in a ratio of 10:1 " +
                    "therefore the amount must be divisible by 10.").ConfigureAwait(false);
            }
            else if (!dairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You need to purchase a dairy first.").ConfigureAwait(false);
            }
            else if (!dairyService.CanMilkFitInDairy(ctx.User.Id, milk))
            {
                await ctx.Channel.SendMessageAsync($"There is not enough room in your dairy for {milk} lbs of milk").ConfigureAwait(false);
            }
            else
            {
                Random random = new Random();
                if (random.Next(0, 70) == 50)
                {
                    await ctx.Channel.SendMessageAsync("Unfortunately something has gone wrong in the cheese making process");
                }
                else
                {
                    _ = SendAndPostRespone(ctx, $"http://localhost:8080/dairy/{ctx.User.Id}/add/milk/{milk}");
                }
            }
        }

        private async Task SendAndPostRespone(CommandContext ctx, string url)
        {
            // send and post respone from api here
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var stringResponse = reader.ReadToEnd();

                await ctx.Channel.SendMessageAsync(stringResponse).ConfigureAwait(false);
            }
        }
    }
}

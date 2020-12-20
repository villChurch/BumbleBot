using System;
using System.Threading.Tasks;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BumbleBot.Commands.Game
{
    [Group("dairy")]
    //[ModuleLifespan(ModuleLifespan.Transient)]
    public class Dairy : BaseCommandModule
    {
        private DairyService dairyService { get; }
        public Dairy(DairyService dairyService)
        {
            this.dairyService = dairyService;
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
                _ = SendAndPostRespone(ctx, "url here");
            }
        }

        private async Task SendAndPostRespone(CommandContext ctx, string url)
        {
            // send and post respone from api here
        }
    }
}

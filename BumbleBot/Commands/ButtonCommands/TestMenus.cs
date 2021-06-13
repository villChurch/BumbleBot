using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

namespace BumbleBot.Commands.ButtonCommands
{
    public class TestMenus : BaseCommandModule
    {

        [Command("tmenu")]
        [Hidden]
        public async Task TestMenuCommand(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var builder = new DiscordMessageBuilder();
            builder.WithContent("This is a test for menus!");

            var select = new DiscordSelectComponent()
            {
                CustomId = "testmenu",
                Placeholder = "Poggers",
                Options = new[]
                {
                    new DiscordSelectComponentOption("Label 1", "option 1"),
                    new DiscordSelectComponentOption("Label 2", "option 2")
                }
            };
            var btn1 = new DiscordButtonComponent(ButtonStyle.Primary, "no1", "Button 1!", true);
            var btn2 = new DiscordButtonComponent(ButtonStyle.Secondary, "no2", "Button 2!", true);
            var btn3 = new DiscordButtonComponent(ButtonStyle.Success, "no3", "Button 3!", true);
            builder.AddComponents(btn1, btn2, btn3);
            builder.AddComponents(select);
            var msg = await builder.SendAsync(ctx.Channel).ConfigureAwait(false);
            var res = await interactivity.WaitForSelectAsync(msg, "testmenu", TimeSpan.FromMinutes(1));

            if (res.TimedOut)
            {
                await ctx.RespondAsync("Sorry but the menu has timed out");
            }
            else
            {
                await ctx.RespondAsync($"You selected {string.Join(", ", res.Result.Values)}").ConfigureAwait(false);
            }
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Interactivity.Extensions;

namespace BumbleBot.Commands.AdminCommands
{
    public class InfoCommand : ApplicationCommandsModule
    {

        [SlashCommand("info_add", "Add a new info prompt")]
        [OwnerOrPermissionSlash(DisCatSharp.Permissions.KickMembers)]
        public async Task AddNewInfo(InteractionContext ctx, [Option("name", "name for info prompt")] string name)
        {
            var id = RandomID(8);
            await ctx.CreateModalResponseAsync(new DisCatSharp.Entities.DiscordInteractionModalBuilder()
                .WithCustomId(id)
                .WithTitle("Info prompt to add")
                .AddModalComponents(new DisCatSharp.Entities.DiscordTextComponent(DisCatSharp.Enums.TextComponentStyle.Small,
                RandomID(8),"Name"))
                .AddModalComponents(new DisCatSharp.Entities.DiscordTextComponent(DisCatSharp.Enums.TextComponentStyle.Paragraph,
                RandomID(8), "Value")));
            var interactivity = ctx.Client.GetInteractivity();
            var result = await interactivity.WaitForModalAsync(id);
            if (result.TimedOut)
            {
                await ctx.EditResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                    .WithContent("Interaction has timed out"));
            }
            else
            {
                var values = result.Result;
            }
        }

        private String RandomID(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}


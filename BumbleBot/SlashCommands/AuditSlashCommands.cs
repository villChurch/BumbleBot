using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BumbleBot.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.EventHandling;
using DisCatSharp.Interactivity.Extensions;

namespace BumbleBot.SlashCommands;

[SlashCommandGroup("audit", "Audit commands")]
public class AuditSlashCommands : ApplicationCommandsModule
{
    private AuditService auditService = new AuditService();

    [SlashCommand("show", "shows last 20 commands executed")]
    public async Task ShowLastTwentyCommandsRun(InteractionContext ctx)
    {
        var commandList = await auditService.GetLastTwentyCommandsRun();
        var pages = new List<Page>();
        var interactivity = ctx.Client.GetInteractivity();
        foreach (var auditCommandEvent in commandList)
        {
            var page = new Page();
            var embed = new DiscordEmbedBuilder
            {
                Title = "Command Executed"
            };
            var user = await ctx.Guild.GetMemberAsync(Convert.ToUInt64(auditCommandEvent.discordId));
            embed.AddField("Command Name", $"{auditCommandEvent.commandName}");
            embed.AddField("Arguments", auditCommandEvent.arguments == string.Empty ?
                "No arguments for this command" : auditCommandEvent.arguments);
            embed.AddField("Run By", $"{user.DisplayName}");
            page.Embed = embed;
            pages.Add(page);
        }

        _ = Task.Run(async () => await interactivity.SendPaginatedResponseAsync(ctx.Interaction, true, ctx.User, pages, null,
                PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable, CancellationToken.None)
            .ConfigureAwait(false));
    }

}
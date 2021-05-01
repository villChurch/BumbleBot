using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.AdminCommands
{
    public class AdminCommands : BaseCommandModule
    {

        [Command("ping")]
        [Description("Ping bot")]
        [RequireOwner]
        [Hidden]
        public async Task GetBotPing(CommandContext ctx)
        {
            await new DiscordMessageBuilder()
                .WithReply(ctx.Message.Id, true)
                .WithContent($"{ctx.Client.Ping}ms")
                .SendAsync(ctx.Channel);
        }

        [Command("fslashcommands")]
        [Description("Fixes when slash commands register twice")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task FSlashCommands(CommandContext ctx)
        {
            await ctx.Client.BulkOverwriteGuildApplicationCommandsAsync(ctx.Guild.Id,
                new List<DiscordApplicationCommand>());
            await ctx.Channel.SendMessageAsync("Duplicate slash commands should now be removed.");
        }
        [Command("info")]
        [Description("Return information about the bot")]
        [Hidden]
        public async Task GetBotInforamtion(CommandContext ctx)
        {
            var ccv = typeof(Bot)
                          .GetTypeInfo()
                          .Assembly
                          ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion ??
                      typeof(Bot)
                          .GetTypeInfo()
                          .Assembly
                          .GetName()
                          .Version?.ToString(3);

            var dsv = ctx.Client.VersionString;
            var ncv = System.Environment.Version.ToString();
            var runTimeVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            var rtv = System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion();
            var embed = new DiscordEmbedBuilder()
            {
                Title = "About Bumblebot",
                Color = DiscordColor.Aquamarine,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                   Text = "Bumblebot is made by VillChurch#2599 (<@!272151652344266762>) and Epona142#6828 (<@!243807523554066433>)"
                }
            };
            embed.AddField("Bot Version", Formatter.Bold(ccv), true);
            embed.AddField("DSharpPlus Version", Formatter.Bold(dsv), true);
            embed.AddField("Net Core version", Formatter.Bold(ncv), true);
            embed.AddField("Net Core full version name", Formatter.Bold(runTimeVer), true);
            embed.AddField("System version", Formatter.Bold(rtv), true);
            
            var upt = DateTime.Now - Process.GetCurrentProcess().StartTime;
            string ups;
            if (upt.Days > 0)
                ups = $@"{upt:%d} days, {upt:hh\:mm\:ss}";
            else ups = upt.ToString(@"hh\:mm\:ss");

            embed.AddField("Uptime", Formatter.InlineCode(ups), true);
            await ctx.Channel.SendMessageAsync(embed).ConfigureAwait(false);
        }
    }
}
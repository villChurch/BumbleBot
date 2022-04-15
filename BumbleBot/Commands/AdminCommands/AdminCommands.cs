using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

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
        
        [Command("info")]
        [Description("Return information about the bot")]
        [Hidden]
        public async Task GetBotInforamtion(CommandContext ctx)
        {
            var ccv = typeof(Bot)
                          .GetTypeInfo()
                          .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion ??
                      typeof(Bot)
                          .GetTypeInfo()
                          .Assembly
                          .GetName()
                          .Version?.ToString(3);

            var dsv = ctx.Client.VersionString;
            var ncv = Environment.Version.ToString();
            var runTimeVer = RuntimeInformation.FrameworkDescription;
            var rtv = RuntimeEnvironment.GetSystemVersion();
            var embed = new DiscordEmbedBuilder
            {
                Title = "About Bumblebot",
                Color = DiscordColor.Aquamarine,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                   Text = "Bumblebot is made by VillChurch#2599 (<@!272151652344266762>) and Epona142#6828 (<@!243807523554066433>)"
                }
            };
            var versionField = new DiscordEmbedField("Bot Version", Formatter.Bold(ccv), true);
            var dcsVersionField = new DiscordEmbedField("DisCatSharp Version", Formatter.Bold(dsv), true);
            var ncVersionField = new DiscordEmbedField("Net Core version", Formatter.Bold(ncv), true);
            var ncVersionFullField =
                new DiscordEmbedField("Net Core full version name", Formatter.Bold(runTimeVer), true);
            var systemVersionField = new DiscordEmbedField("System version", Formatter.Bold(rtv), true);

            var upt = DateTime.Now - Process.GetCurrentProcess().StartTime;
            string ups;
            if (upt.Days > 0)
                ups = $@"{upt:%d} days, {upt:hh\:mm\:ss}";
            else ups = upt.ToString(@"hh\:mm\:ss");

            var upTimeField = new DiscordEmbedField("Uptime", Formatter.InlineCode(ups), true);
            embed.AddFields(new List<DiscordEmbedField>
            {
                versionField,
                dcsVersionField,
                ncVersionField,
                ncVersionFullField,
                systemVersionField,
                upTimeField
            });
            await ctx.Channel.SendMessageAsync(embed).ConfigureAwait(false);
        }
    }
}
using System.Diagnostics;
using System.Net.NetworkInformation;
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

        [Command("status")]
        [Description("Show bot status")]
        [RequireUserPermissions(Permissions.KickMembers)]
        [Hidden]
        public async Task ShowBotStatus(CommandContext ctx)
        {
            PerformanceCounter cpuCounter;
            PerformanceCounter ramCounter;

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            await new DiscordMessageBuilder()
                .WithReply(ctx.Message.Id, true)
                .WithContent($"CPU usage - {cpuCounter.NextValue()}%, RAM usabe - {ramCounter.NextValue()} MB")
                .SendAsync(ctx.Channel);
        }
    }
}
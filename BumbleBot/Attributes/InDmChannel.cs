using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BumbleBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class InDmChannel : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(ctx.Channel.IsPrivate);
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;

namespace BumbleBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class OwnerOrPermission : CheckBaseAttribute
    {
        public OwnerOrPermission(Permissions permissions)
        {
            Permissions = permissions;
        }

        public Permissions Permissions { get; }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var app = ctx.Client.CurrentApplication;

            var me = ctx.Client.CurrentUser;

            if (app != null && app.Owners.Contains(ctx.User))
                return Task.FromResult(true);

            if (ctx.User.Id == me.Id)
                return Task.FromResult(true);

            var usr = ctx.Member;
            if (usr == null)
                return Task.FromResult(false);
            var pusr = ctx.Channel.PermissionsFor(usr);

            return Task.FromResult((pusr & Permissions) == Permissions);
        }
    }
}
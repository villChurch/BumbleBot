using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;

namespace BumbleBot.Attributes;

public class OwnerOrPermissionSlash : SlashCheckBaseAttribute
{
    public OwnerOrPermissionSlash(Permissions permissions)
    {
        this.permissions = permissions;
    }

    private Permissions permissions;
    
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
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

        return Task.FromResult((pusr & permissions) == permissions);
    }
}
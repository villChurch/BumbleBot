using System.Threading.Tasks;
using BumbleBot.Utilities;
using DisCatSharp.ApplicationCommands;

namespace BumbleBot.ApplicationCommands.SlashCommands.Game.GoatSpawns;

public class EnabledSpawns : ApplicationCommandsModule
{
    private DbUtils dbUtils = new();

    [SlashCommand("enable_spawn", "Enables spawning of particular specials")]
    public async Task EnableSpecialVariation(InteractionContext ctx)
    {
        
    }
}
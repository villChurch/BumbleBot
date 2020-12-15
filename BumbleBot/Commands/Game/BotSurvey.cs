using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace BumbleBot.Commands.Game
{
    [Group("survey")]
    [Hidden]
    public class BotSurvey : BaseCommandModule
    {

        [GroupCommand]
        public async Task TakeSurvey(CommandContext ctx)
        {

        }
    }
}

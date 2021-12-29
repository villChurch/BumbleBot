using System;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;

namespace BumbleBot.Commands;

[SlashCommandGroup("top_level", "top level group")]
public class MySlashCommandGroups : ApplicationCommandsModule
{
    [SlashCommandGroup("mid_level", "mid level group")]
    public class MidLevelGroup : ApplicationCommandsModule
    {
        [SlashCommand("test_command", "test command")]
        public async Task TestCommand(InteractionContext ctx,
            [Option("repeat_this", "repeats the text")] string repeatMe)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .WithContent(repeatMe));
        }
    }

    [SlashCommandGroup("mid_level_two", "mid level group two")]
    public class MidLevelGroupTwo : ApplicationCommandsModule
    {
        [SlashCommand("reverse_test", "test command to reverse a string")]
        public async Task TestReverseCommand(InteractionContext ctx,
            [Option("reverse_this", "reverses the text")] string reverseMe)
        {
            char[] charArray = reverseMe.ToCharArray();
            Array.Reverse( charArray );
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent(new string(charArray)));
        } 
    }
}
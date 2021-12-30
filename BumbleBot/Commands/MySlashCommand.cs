using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.Entities;

namespace BumbleBot.Commands;

public class MySlashCommand : ApplicationCommandsModule
{
    public class MyAutocompleteProvider : IAutocompleteProvider
    {
        public Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
        {
            var options = new List<DiscordApplicationCommandAutocompleteChoice>
            {
                new DiscordApplicationCommandAutocompleteChoice("First option", "first"),
                new DiscordApplicationCommandAutocompleteChoice("Second option", "second"),
                new DiscordApplicationCommandAutocompleteChoice("Guild_Name", context.Guild.Name)
            };
            
            return Task.FromResult(options.AsEnumerable());
        }
    }
    [SlashCommandGroup("my_command", "This is decription of the command group.")]
    public class MyCommandGroup : ApplicationCommandsModule
    {
        [SlashCommand("first", "This is decription of the command.")]
        public async Task MySlashCommand(InteractionContext context,
            [Option("test", "test")] string test, [Choice("optional", "optional")] [Option("optional", "optional")] string optional = null)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = "This is first subcommand."
            });
        }
        [SlashCommand("second", "This is decription of the command.")]
        public async Task MySecondCommand(InteractionContext context, [Autocomplete(typeof(MyAutocompleteProvider))]
            [Option("repeat", "String to repeat", true)] string repeatMe)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            {
                Content = repeatMe
            });
        }
        
        public class DefaultHelpAutoCompleteProvider : IAutocompleteProvider
        {
            public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
            {
                var options = new List<DiscordApplicationCommandAutocompleteChoice>();
                var globalCommands = context.ApplicationCommandsExtension.GlobalCommands.Count > 0 ? context.ApplicationCommandsExtension.GlobalCommands : new List<DiscordApplicationCommand>();
                var guildCommands = context.ApplicationCommandsExtension.GuildCommands[context.Guild.Id].Count > 0
                    ? context.ApplicationCommandsExtension.GuildCommands[context.Guild.Id]
                    : new List<DiscordApplicationCommand>();
                var slashCommands = globalCommands.Concat(guildCommands)
                    .Where(ac => !ac.Name.Equals("help", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(ac => ac.Name).Select(x => x.First()).Where(ac => ac.Name.StartsWith(context.Options[0].Value.ToString(), StringComparison.OrdinalIgnoreCase));
                var list = slashCommands.ToList();
                foreach (var sc in list.Take(25))
                {
                    options.Add(new DiscordApplicationCommandAutocompleteChoice(sc.Name, sc.Name.Trim()));
                }
                return options.AsEnumerable();
            }
        }

        [SlashCommand("search", "Searches slash commands")]
        public async Task SearchSlashCommands(InteractionContext context,
            [Autocomplete(typeof(DefaultHelpAutoCompleteProvider))] [Option("value", "value to search for", true)]
            string value)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"Found {value}").AsEphemeral(true));
        }
    }
}
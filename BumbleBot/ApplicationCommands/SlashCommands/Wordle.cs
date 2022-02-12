using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace BumbleBot.ApplicationCommands.SlashCommands;

public class Wordle : ApplicationCommandsModule
{
    private WordleLists wordleLists = new();
    [SlashCommand("wordle", "starts a game of wordle")]
    public async Task PlayWordle(InteractionContext ctx)
    {
        #region ActionRows
        var blank_emoji = new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, ":question:"));
        var initialRow = new List<DiscordComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Primary, "1_1", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "1_2", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "1_3", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "1_4", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "1_5", "", true, blank_emoji)
        };
        var rowTwo = new List<DiscordComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Primary, "2_1", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "2_2", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "2_3", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "2_4", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "2_5", "", true, blank_emoji)
        };
        var rowThree = new List<DiscordComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Primary, "3_1", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "3_2", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "3_3", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "3_4", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "3_5", "", true, blank_emoji)
        };
        var rowFour = new List<DiscordComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Primary, "4_1", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "4_2", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "4_3", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "4_4", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "4_5", "", true, blank_emoji)
        };
        var rowFive = new List<DiscordComponent>
        {
            new DiscordButtonComponent(ButtonStyle.Primary, "5_1", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "5_2", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "5_3", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "5_4", "", true, blank_emoji),
            new DiscordButtonComponent(ButtonStyle.Primary, "5_5", "", true, blank_emoji)
        };
        #endregion
        var word = GetWord();
        var ar = new DiscordActionRowComponent(initialRow);
        var arList = new List<DiscordActionRowComponent>
        {
            ar,
            new(rowTwo),
            new(rowThree),
            new(rowFour),
            new(rowFive)
        };
        ctx.Client.Logger.LogInformation("Word is {word}", word);
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder());
        await ctx.EditResponseAsync(new DiscordWebhookBuilder()
            .WithContent("Please type your response")
            .AddComponents(arList));
        int counter = 0;
        var interactivity = ctx.Client.GetInteractivity();
        do
        {
            var correctLetters = new List<char>();
            var msg = await interactivity.WaitForMessageAsync(x => x.Author.Id == ctx.User.Id && x.Content.Trim().Length == 5, TimeSpan.FromMinutes(5));
            if (msg.TimedOut)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent($"Game has ended due to response taking longer than five minutes. The word was {word}"));
                counter = 60;
            }
            /*
             *                 else if (msg.Result.Content.ToLower().Equals(word))
                {
                    await msg.Result.DeleteAsync();
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"You guessed the word correctly with {word}")
                        //.AddComponents(arList));
                        );
                    counter = 60;
                }
             */
            else
            {
                var resultWord = msg.Result.Content.ToLower();
                var editRow = new List<DiscordComponent>();
                //regional_indicator_
                for (int i = 0; i < 5; i++)
                {
                    if (resultWord[i].Equals(word[i]))
                    {
                        editRow.Add(new DiscordButtonComponent(ButtonStyle.Success, $"{counter.ToString()}_{i.ToString()}","", true,
                            new DiscordComponentEmoji(DiscordEmoji.FromName(
                                ctx.Client, $":regional_indicator_{resultWord[i]}:"))));
                        correctLetters.Add(resultWord[i]);
                        if (correctLetters.Count(x => x.Equals(resultWord[i])) >
                            word.ToCharArray().Count(x => x.Equals(resultWord[i])))
                        {
                            for (int j = 0; j <= i; j++)
                            {
                                DiscordButtonComponent rowComponent = (DiscordButtonComponent)editRow[j];
                                var letter = ReturnLetterFromEmoji(rowComponent.Emoji); //.Name.Replace(":regional_indicator_", "").Replace(":", ""));
                                if (letter.Equals(resultWord[i]))
                                {
                                    var dbc = new DiscordButtonComponent(ButtonStyle.Primary, $"{counter.ToString()}_{i.ToString()}","",true,
                                        new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, $":regional_indicator_{resultWord[i]}:")));
                                    editRow[j] = dbc;
                                }
                            }
                        }
                    }
                    else if (word.Contains(resultWord[i]) && word.ToCharArray().Count(x => x.Equals(resultWord[i])) != correctLetters.Count(x => x.Equals(resultWord[i])))
                    {
                        editRow.Add(new DiscordButtonComponent(ButtonStyle.Danger, $"{counter.ToString()}_{i.ToString()}","",true,
                            new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, $":regional_indicator_{resultWord[i]}:"))));
                        correctLetters.Add(resultWord[i]);
                    }
                    else
                    {
                        editRow.Add(new DiscordButtonComponent(ButtonStyle.Primary, $"{counter.ToString()}_{i.ToString()}","",true,
                            new DiscordComponentEmoji(DiscordEmoji.FromName(ctx.Client, $":regional_indicator_{resultWord[i]}:"))));
                    }
                }

                arList[counter] = new DiscordActionRowComponent(editRow);
                await msg.Result.DeleteAsync();
                if (msg.Result.Content.ToLower().Equals(word))
                {
                    await msg.Result.DeleteAsync();
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"You guessed the word correctly with {word}")
                        .AddComponents(arList));
                    counter = 60;
                }
                else if (counter == 4)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"Incorrect. Correct answer was {word}")
                        .AddComponents(arList));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Incorrect.")
                        .AddComponents(arList));
                }
                counter++;
            }
        } while (counter < 5);
    }

    private char ReturnLetterFromEmoji(DiscordComponentEmoji emoji)
    {
        return emoji.Name.ToLower() switch
        {
            ":regional_indicator_a:" => 'a',
            ":regional_indicator_b:" => 'b',
            ":regional_indicator_c:" => 'c',
            ":regional_indicator_d:" => 'd',
            "regional_indicator_e:" => 'e',
            ":regional_indicator_f:" => 'f',
            ":regional_indicator_g:" => 'g',
            ":regional_indicator_h:" => 'h',
            ":regional_indicator_i:" => 'i',
            ":regional_indicator_j:" => 'j',
            ":regional_indicator_k:" => 'k',
            ":regional_indicator_l:" => 'l',
            ":regional_indicator_m:" => 'm',
            ":regional_indicator_n:" => 'n',
            ":regional_indicator_o:" => 'o',
            ":regional_indicator_p:" => 'p',
            ":regional_indicator_q:" => 'q',
            ":regional_indicator_r:" => 'r',
            ":regional_indicator_s:" => 's',
            ":regional_indicator_t:" => 't',
            ":regional_indicator_u:" => 'u',
            ":regional_indicator_v:" => 'v',
            ":regional_indicator_w:" => 'w',
            ":regional_indicator_x:" => 'x',
            ":regional_indicator_y:" => 'y',
            ":regional_indicator_z:" => 'z',
            _ => '?'
        };
    }
    private String GetWord()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        List<string> extraWordList = System.IO.File.ReadLines(path+"/wordleWords.txt").ToList();
        var wordList = wordleLists.words;
        wordList.AddRange(extraWordList);
        return wordList[new Random().Next(wordList.Count)].ToLower();
    }

}
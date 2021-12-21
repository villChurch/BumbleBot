using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

namespace BumbleBot.Commands.GoatGames
{
    public class GuessWho : BaseCommandModule
    {
        [Command("guesswho")]
        [Aliases("gw")]
        public async Task GuessWhoCommand(CommandContext ctx)
        {
            List<String> goats = new List<string>()
            {
                "JP",
                "alibi",
                "amanita",
                "angel",
                "avalanche",
                "baby",
                "barra",
                "beetle",
                "bluecedar",
                "bug",
                "bumble",
                "buttons2",
                "catnip",
                "cherry",
                "cilantro",
                "citation",
                "commando",
                "crimini",
                "crownroyal",
                "dazzle",
                "diamond",
                "dumpling",
                "eevee",
                "emily",
                "ester",
                "fanny2",
                "fennel",
                "fiesta",
                "gigi",
                "glitch",
                "hatsumi",
                "hawthorne",
                "hottoddy",
                "hyssop",
                "inky",
                "juliet",
                "junerose",
                "kiki",
                "larkspur",
                "lavender",
                "lime",
                "lucy",
                "macy",
                "maggie",
                "mandarin",
                "marjoram",
                "mattie",
                "midori",
                "minx",
                "mocha",
                "moony",
                "mustard",
                "myrtle",
                "nettles",
                "patch",
                "peyote",
                "photoshop",
                "pickles",
                "pinky",
                "porcini",
                "rainy",
                "rocket",
                "saffron",
                "scary",
                "sissy",
                "sparklez",
                "spike",
                "stormy",
                "summer",
                "sunsetstrip",
                "thunder",
                "tia",
                "vixen",
                "zenyatta",
                "zira"
            };
            var selectedGoatNumber = new Random().Next(0, goats.Count);
            var selectedGoat = goats[selectedGoatNumber];
            var imagePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Goats/{selectedGoat}.png";
            selectedGoat = Regex.Replace(selectedGoat, @"[\d-]", string.Empty);
            goats.RemoveAt(selectedGoatNumber);
            var secondGoat = goats[new Random().Next(0, goats.Count)];
            secondGoat = Regex.Replace(secondGoat, @"[\d-]", string.Empty);
            goats.Remove(secondGoat);
            var thirdGoat = goats[new Random().Next(0, goats.Count)];
            thirdGoat = Regex.Replace(thirdGoat, @"[\d-]", string.Empty);
            var goatNames = new List<String>()
            {
                selectedGoat,
                secondGoat,
                thirdGoat
            };
            var goatOne = goatNames[new Random().Next(0, goatNames.Count)];
            var firstButton = new DiscordButtonComponent(ButtonStyle.Secondary, goatOne, goatOne.ToUpperInvariant());
            goatNames.Remove(goatOne);
            var goatTwo = goatNames[new Random().Next(0, goatNames.Count)];
            var secondButton = new DiscordButtonComponent(ButtonStyle.Secondary, goatTwo, goatTwo.ToUpperInvariant());
            goatNames.Remove(goatTwo);
            var goatThree = goatNames[0];
            var thirdButton =
                new DiscordButtonComponent(ButtonStyle.Secondary, goatThree, goatThree.ToUpperInvariant());
            var fileStream = File.OpenRead(imagePath);
            var msg = await new DiscordMessageBuilder()
                .WithContent("Who is the following goat?")
                .WithFile(fileStream)
                .AddComponents(firstButton, secondButton, thirdButton)
                .SendAsync(ctx.Channel).ConfigureAwait(false);
            var interactivity = ctx.Client.GetInteractivity();
            var guessWhoReaction =
                await interactivity.WaitForButtonAsync(msg, TimeSpan.FromMinutes(1))
                    .ConfigureAwait(false);
            if (guessWhoReaction.TimedOut)
            {
                await msg.ModifyAsync("Guess who timed out").ConfigureAwait(false);
            }

            if (guessWhoReaction.Result.Id == selectedGoat)
            {
                await guessWhoReaction.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                $"{guessWhoReaction.Result.User.Mention} got the answer correct with " +
                                $"{guessWhoReaction.Result.Id.ToUpperInvariant()}"))
                    .ConfigureAwait(false);
            }
            else
            {
                await guessWhoReaction.Result.Interaction.CreateResponseAsync(
                        InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                $"Unfortunately {guessWhoReaction.Result.User.Mention} selected " +
                                $"{guessWhoReaction.Result.Id.ToUpperInvariant()} when the correct answer was " +
                                $"{selectedGoat.ToUpperInvariant()}"))
                    .ConfigureAwait(false);
            }
        }
    }
}
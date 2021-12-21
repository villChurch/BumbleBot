using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity.Extensions;

namespace BumbleBot.Commands.QuizCommands
{
    [Group("Quiz")]
    [Hidden]
    [RequireOwner]
    public class Quiz : BaseCommandModule
    {
        [Command("add")]
        [Hidden]
        [RequireOwner]
        [Description("Starts a dialogue to create a poll")]
        public async Task AddQuestion(CommandContext ctx, [Description("Channel for poll to be posted in")]
            DiscordChannel channel)
        {
            var interactivity = ctx.Client.GetInteractivity();
            await ctx.Channel.SendMessageAsync("Please enter the question to ask").ConfigureAwait(false);
            var questionResponse = await interactivity.WaitForMessageAsync(
                    x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel, TimeSpan.FromMinutes(5))
                .ConfigureAwait(false);
            if (questionResponse.TimedOut)
            {
                await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                return;
            }

            var question = questionResponse.Result.Content;

            var multipleChoiceMsg = await ctx.Channel.SendMessageAsync("Is this question multiple choice?")
                .ConfigureAwait(false);
            var thumbsup = DiscordEmoji.FromName(ctx.Client, ":thumbsup:");
            var thumbsdown = DiscordEmoji.FromName(ctx.Client, ":thumbsdown:");
            await multipleChoiceMsg.CreateReactionAsync(thumbsup);
            await multipleChoiceMsg.CreateReactionAsync(thumbsdown);
            var multipleChoiceResponse = await interactivity.WaitForReactionAsync(reaction => reaction.Message ==
                    multipleChoiceMsg && reaction.User == ctx.User &&
                    (reaction.Emoji == thumbsdown || reaction.Emoji == thumbsup),
                TimeSpan.FromMinutes(5)).ConfigureAwait(false);

            if (multipleChoiceResponse.TimedOut)
            {
                await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                return;
            }

            if (multipleChoiceResponse.Result.Emoji == thumbsup)
            {
                await ctx.Channel.SendMessageAsync("Multiple choice messages have three possible answers. " +
                                                   "Please type the first answer.").ConfigureAwait(false);
                var answerOneResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author
                    && x.Channel == ctx.Channel, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (answerOneResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }

                var answerOne = answerOneResponse.Result.Content;

                await ctx.Channel.SendMessageAsync("Please type the second answer.").ConfigureAwait(false);
                var answerTwoResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author
                    && x.Channel == ctx.Channel, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (answerTwoResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }

                var answerTwo = answerTwoResponse.Result.Content;

                await ctx.Channel.SendMessageAsync("Please type the third answer.").ConfigureAwait(false);
                var answerThreeResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author
                    && x.Channel == ctx.Channel, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (answerThreeResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }

                var answerThree = answerThreeResponse.Result.Content;

                var a = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_a:");
                var b = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_b:");
                var c = DiscordEmoji.FromName(ctx.Client, ":regional_indicator_c:");
                var correctAnswerMsg = await ctx.Channel
                    .SendMessageAsync("Please react A B or C depending on which is the correct answer.")
                    .ConfigureAwait(false);
                await correctAnswerMsg.CreateReactionAsync(a).ConfigureAwait(false);
                await correctAnswerMsg.CreateReactionAsync(b).ConfigureAwait(false);
                await correctAnswerMsg.CreateReactionAsync(c).ConfigureAwait(false);
                var correctAnswerRespone = await interactivity.WaitForReactionAsync(x =>
                        x.Message == correctAnswerMsg && x.User == ctx.User
                                                      && (x.Emoji == a || x.Emoji == b || x.Emoji == c),
                    TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (correctAnswerRespone.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }

                var questionEmbed = new DiscordEmbedBuilder
                {
                    Title = $"{question}",
                    Color = DiscordColor.Aquamarine,
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = $"Created by {ctx.Member.DisplayName}"
                    }
                };
                questionEmbed.AddField(DiscordEmoji.FromName(ctx.Client, ":regional_indicator_a:"), answerOne);
                questionEmbed.AddField(DiscordEmoji.FromName(ctx.Client, ":regional_indicator_b:"), answerTwo);
                questionEmbed.AddField(DiscordEmoji.FromName(ctx.Client, ":regional_indicator_c:"), answerThree);

                var questionEmbedMsg = await channel.SendMessageAsync(embed: questionEmbed).ConfigureAwait(false);
                await questionEmbedMsg.CreateReactionAsync(a).ConfigureAwait(false);
                await questionEmbedMsg.CreateReactionAsync(b).ConfigureAwait(false);
                await questionEmbedMsg.CreateReactionAsync(c).ConfigureAwait(false);
                var correctAnswer = await interactivity.WaitForReactionAsync(x => x.Message == questionEmbedMsg &&
                    x.Emoji == correctAnswerRespone.Result.Emoji, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (correctAnswer.TimedOut)
                {
                    await channel.SendMessageAsync("No one got the correct answer this time").ConfigureAwait(false);
                }
                else
                {
                    var winner = await ctx.Guild.GetMemberAsync(correctAnswer.Result.User.Id).ConfigureAwait(false);
                    await channel
                        .SendMessageAsync($"Congratulations {winner.DisplayName} you got the correct answer first")
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await ctx.Channel.SendMessageAsync("What is the answer to your question?").ConfigureAwait(false);
                var answerResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author
                    && x.Channel == ctx.Channel, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (answerResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("Command has timed out").ConfigureAwait(false);
                    return;
                }

                var answer = answerResponse.Result.Content;

                var questionEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Question from {ctx.Member.DisplayName}",
                    Description = $"{question}",
                    Color = DiscordColor.Aquamarine
                };

                await channel.SendMessageAsync(embed: questionEmbed).ConfigureAwait(false);

                var singularResponse = await interactivity.WaitForMessageAsync(x => x.Channel == channel
                        && x.Content.Trim().ToLower() == answer.Trim().ToLower(), TimeSpan.FromMinutes(1))
                    .ConfigureAwait(false);
                if (singularResponse.TimedOut)
                {
                    await channel.SendMessageAsync($"No one got the correct answer of {answer}").ConfigureAwait(false);
                }
                else
                {
                    var winner = await ctx.Guild.GetMemberAsync(singularResponse.Result.Author.Id);
                    await channel.SendMessageAsync($"Congratulations {winner.DisplayName} you got the correct answer!")
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
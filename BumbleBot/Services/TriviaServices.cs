using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Bumblebot.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Newtonsoft.Json;

namespace BumbleBot.Services
{
    public class TriviaServices
    {
        private Timer timer;
        private Timer triviaQuestionTimer;

        private bool TriviaRunning { get; set; }
        private bool QuestionTimerRunning { get; set; }

        public TriviaQuestions GetQuestionsAsync()
        {
            string json;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            using (var fs =
                File.OpenRead(path + "/questions.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<TriviaQuestions>(json);
        }

        private void SetTimer(CommandContext ctx, DiscordChannel channel)
        {
            timer = new Timer(10000);
            timer.Start();
            timer.Elapsed += (sender, e) => StartTriviaTimer(sender, e, ctx, channel);
            timer.Enabled = true;
            TriviaRunning = true;
        }

        private void StartTriviaTimer(object source, ElapsedEventArgs e, CommandContext ctx, DiscordChannel channel)
        {
            triviaQuestionTimer = new Timer(30000); //120000);
            triviaQuestionTimer.Start();
            triviaQuestionTimer.Elapsed += (sender, ee) => TriviaTimerHandler(sender, ee, ctx, channel);
            timer.Stop();
            timer.Dispose();
            _ = Task.Run(() => AskQuestion(ctx, channel));
        }

        private void TriviaTimerHandler(object source, ElapsedEventArgs e, CommandContext ctx, DiscordChannel channel)
        {
            _ = Task.Run(() => AskQuestion(ctx, channel));
            QuestionTimerRunning = false;
        }

        public bool StartCountdownTriviaTimer(CommandContext ctx, DiscordChannel channel)
        {
            if (TriviaRunning)
            {
                return false;
            }

            SetTimer(ctx, channel);
            return true;
        }

        public bool StopTrivia()
        {
            if (TriviaRunning)
            {
                triviaQuestionTimer.Stop();
                triviaQuestionTimer.Dispose();
                TriviaRunning = false;
                QuestionTimerRunning = false;
                return true;
            }

            return false;
        }

        public async Task AskQuestion(CommandContext ctx, DiscordChannel channel)
        {
            try
            {
                var questions = GetQuestionsAsync();
                var random = new Random();
                var questionNumber = random.Next(0, questions.Questions.Length);

                var embed = new DiscordEmbedBuilder
                {
                    Title = questions.Questions[questionNumber].QuestionQuestion
                };
                var answers = questions.Questions[questionNumber].IncorrectAnswers.Length + 1;
                var correctAnswer = random.Next(0, answers); // for counter
                var count = 0;
                var charCounter = 0;
                var answer = correctAnswer;
                while (count < answers)
                {
                    var character = Convert.ToChar(charCounter + 65);
                    if (count == correctAnswer)
                    {
                        embed.AddField(character.ToString(), questions.Questions[questionNumber].CorrectAnswer);
                        answers--;
                        correctAnswer = -1;
                    }
                    else
                    {
                        var answerString = questions.Questions[questionNumber].IncorrectAnswers[count].Bool == null
                            ? questions.Questions[questionNumber].IncorrectAnswers[count].String
                            : questions.Questions[questionNumber].IncorrectAnswers[count].Bool.ToString();
                        embed.AddField(character.ToString(), answerString);
                        count++;
                    }

                    charCounter++;
                }

                var msg = await channel.SendMessageAsync(embed: embed);
                var alphaReactionCommon = ":regional_indicator_";
                var emojis = new List<DiscordEmoji>();
                for (var i = 0; i <= answers; i++)
                {
                    var character = Convert.ToChar(i + 65);
                    var emoji = DiscordEmoji.FromName(ctx.Client,
                        $"{alphaReactionCommon}{character.ToString().ToLower()}:");
                    await msg.CreateReactionAsync(emoji);
                    emojis.Add(emoji);
                }

                var interactivity = ctx.Client.GetInteractivity();
                var characterr = Convert.ToChar(answer + 65);
                var correctAnswerEmoji = DiscordEmoji.FromName(ctx.Client,
                    $"{alphaReactionCommon}{characterr.ToString().ToLower()}:");
                var voted = new HashSet<DiscordUser>();
                QuestionTimerRunning = true;
                while (QuestionTimerRunning)
                {
                    var response = await interactivity.WaitForReactionAsync(
                        x => x.Message == msg && emojis.Contains(x.Emoji) && x.User.IsBot == false,
                        TimeSpan.FromMinutes(1));
                    if (response.Result != null && response.Result.User != null)
                    {
                        if (!voted.Contains(response.Result.User))
                        {
                            if (response.Result.Emoji == correctAnswerEmoji)
                            {
                                await channel.SendMessageAsync(
                                        $"Congratulations {response.Result.User.Mention} you got the right answer with " +
                                        $"{characterr}")
                                    .ConfigureAwait(false);
                                QuestionTimerRunning = false;
                            }
                            else
                            {
                                voted.Add(response.Result.User);
                            }
                        }
                        else
                        {
                            await msg.DeleteReactionAsync(response.Result.Emoji, response.Result.User,
                                "Voted more than once");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ctx.Channel
                    .SendMessageAsync("An error has occured while running trivia so trivia has been stopped")
                    .ConfigureAwait(false);
                triviaQuestionTimer.Stop();
                triviaQuestionTimer.Dispose();
                QuestionTimerRunning = false;
                TriviaRunning = false;
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
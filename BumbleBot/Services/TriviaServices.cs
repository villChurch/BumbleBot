using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private bool triviaRunning { get; set; }
        private bool questionTimerRunning { get; set; }
        private Timer timer;
        private Timer triviaQuestionTimer;

        public TriviaServices()
        {
        }

        public TriviaQuestions GetQuestionsAsync()
        {
            string json;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            using (var fs =
                File.OpenRead(path + "/questions.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();

            return JsonConvert.DeserializeObject<TriviaQuestions>(json);
        }

        private void SetTimer(CommandContext ctx, DiscordChannel channel)
        {
            timer = new Timer(10000);
            timer.Start();
            timer.Elapsed += (sender, e) => StartTriviaTimer(sender, e, ctx, channel);
            timer.Enabled = true;
            triviaRunning = true;
        }

        private void StartTriviaTimer(Object source, ElapsedEventArgs e, CommandContext ctx, DiscordChannel channel)
        {
            triviaQuestionTimer = new Timer(60000); //120000);
            triviaQuestionTimer.Start();
            triviaQuestionTimer.Elapsed += (sender, ee) => TriviaTimerHandler(sender, ee, ctx, channel);
            timer.Stop();
            timer.Dispose();
            _ = AskQuestion(ctx, channel);
        }

        private void TriviaTimerHandler(Object source, ElapsedEventArgs e, CommandContext ctx, DiscordChannel channel)
        {
            _ = AskQuestion(ctx, channel);
            questionTimerRunning = false;
        }

        public bool StartCountdownTriviaTimer(CommandContext ctx, DiscordChannel channel)
        {
            if (triviaRunning)
            {
                return false;
            }
            else
            {
                SetTimer(ctx, channel);
                return true;
            }
        }

        public bool StopTrivia()
        {
            if (triviaRunning)
            {
                triviaQuestionTimer.Stop();
                triviaQuestionTimer.Dispose();
                triviaRunning = false;
                questionTimerRunning = false;
                return true;
            }
            return false;
        }

        public async Task AskQuestion(CommandContext ctx, DiscordChannel channel)
        {
            try
            {
                TriviaQuestions questions = GetQuestionsAsync();
                Random random = new Random();
                int questionNumber = random.Next(0, questions.Questions.Length);

                var embed = new DiscordEmbedBuilder
                {
                    Title = questions.Questions[questionNumber].QuestionQuestion
                };
                int answers = questions.Questions[questionNumber].IncorrectAnswers.Length + 1;
                int correctAnswer = random.Next(0, answers); // for counter
                int count = 0;
                int charCounter = 0;
                int answer = correctAnswer;
                while (count < answers)
                {
                    char character = Convert.ToChar(charCounter + 65);
                    if (count == correctAnswer)
                    {
                        embed.AddField(character.ToString(), questions.Questions[questionNumber].CorrectAnswer);
                        answers--;
                        correctAnswer = -1;
                    }
                    else
                    {
                        var answerString = questions.Questions[questionNumber].IncorrectAnswers[count].Bool == null ?
                            questions.Questions[questionNumber].IncorrectAnswers[count].String : questions.Questions[questionNumber].IncorrectAnswers[count].Bool.ToString();
                        embed.AddField(character.ToString(), answerString);
                        count++;
                    }
                    charCounter++;
                }
                var msg = await channel.SendMessageAsync(embed: embed);
                string alphaReactionCommon = ":regional_indicator_";
                List<DiscordEmoji> emojis = new List<DiscordEmoji>();
                for (int i = 0; i <= answers; i++)
                {
                    char character = Convert.ToChar(i + 65);
                    DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, $"{alphaReactionCommon}{character.ToString().ToLower()}:");
                    await msg.CreateReactionAsync(emoji);
                    emojis.Add(emoji);
                }
                var interactivity = ctx.Client.GetInteractivity();
                var characterr = Convert.ToChar(answer + 65);
                var correctAnswerEmoji = DiscordEmoji.FromName(ctx.Client, $"{alphaReactionCommon}{characterr.ToString().ToLower()}:");
                HashSet<DiscordUser> voted = new HashSet<DiscordUser>();
                questionTimerRunning = true;
                while (questionTimerRunning)
                {
                    var response = await interactivity.WaitForReactionAsync(x => x.Message == msg && emojis.Contains(x.Emoji) && x.User.IsBot == false, TimeSpan.FromMinutes(1));
                    if (response.Result != null && response.Result.User != null)
                    {
                        if (!voted.Contains(response.Result.User))
                        {
                            if (response.Result.Emoji == correctAnswerEmoji)
                            {
                                await channel.SendMessageAsync($"Congratulations {response.Result.User.Mention} you got the right answer with " +
                                    $"{characterr}")
                                    .ConfigureAwait(false);
                                questionTimerRunning = false;
                            }
                            else
                            {
                                voted.Add(response.Result.User);
                            }
                        }
                        else
                        {
                            await msg.DeleteReactionAsync(response.Result.Emoji, response.Result.User, "Voted more than once");
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync("An error has occured while running trivia so trivia has been stopped").ConfigureAwait(false);
                triviaQuestionTimer.Stop();
                triviaQuestionTimer.Dispose();
                questionTimerRunning = false;
                triviaRunning = false;
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}

using System;
using BumbleBot.Services;
using BumbleBot.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using Bumblebot.Models;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Interactivity;
using DSharpPlus.EventArgs;
using System.Timers;

namespace BumbleBot.Commands.Trivia
{
    [Group("trivia")]
    [OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
    public class MainTriviaCommands : BaseCommandModule
    {

        TriviaServices triviaServices { get; }
        Timer timer;
        bool questionTimerRunning = false;

        public MainTriviaCommands(TriviaServices triviaServices)
        {
            this.triviaServices = triviaServices;
        }

        private void SetTimer()
        {
            timer = new Timer(120000 / 4);
            timer.Elapsed += FinishTimer;
            timer.Enabled = true;
            questionTimerRunning = true;
        }

        private void FinishTimer(Object source, ElapsedEventArgs e)
        {
            questionTimerRunning = false;
            timer.Stop();
            timer.Dispose();
        }

        [Command("count"), OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        public async Task GetNumberOfQuestions(CommandContext ctx)
        {
            TriviaQuestions questions = triviaServices.GetQuestionsAsync();
            
            await ctx.Channel.SendMessageAsync($"There are {questions.Questions.Length} questions").ConfigureAwait(false);
        }

        [Command("random"), OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        public async Task GetRandomQuestion(CommandContext ctx)
        {
            await triviaServices.AskQuestion(ctx, ctx.Channel);   
        }

        [Command("start")]
        [OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        [Description("Starts a trivia game")]
        public async Task StartTriviaGame(CommandContext ctx, DiscordChannel channel)
        {
            bool started = triviaServices.StartCountdownTriviaTimer(ctx, channel);

            string message = started == true ? $"Trivia wil now start in {channel.Mention} in 10 seconds" :
                $"Trivia could not be started, this could be due to a game already being run";

            await ctx.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }

        [Command("stop")]
        [OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        [Description("Stops a trivia game")]
        public async Task StopTriviaGame(CommandContext ctx)
        {
            bool stopped = triviaServices.StopTrivia();

            string message = stopped == true ? $"Trivia has been stopped" : $"No trivia found to be stopped";

            await ctx.Channel.SendMessageAsync(message);
        }

        
    }
}

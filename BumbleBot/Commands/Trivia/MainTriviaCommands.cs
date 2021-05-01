using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.Trivia
{
    [Group("trivia")]
    [OwnerOrPermission(Permissions.KickMembers)]
    public class MainTriviaCommands : BaseCommandModule
    {
        public MainTriviaCommands(TriviaServices triviaServices)
        {
            this.TriviaServices = triviaServices;
        }

        private TriviaServices TriviaServices { get; }

        [Command("count")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task GetNumberOfQuestions(CommandContext ctx)
        {
            var questions = TriviaServices.GetQuestionsAsync();

            await ctx.Channel.SendMessageAsync($"There are {questions.Questions.Length} questions")
                .ConfigureAwait(false);
        }

        [Command("random")]
        [Description("Asks a random trivia question")]
        public async Task GetRandomQuestion(CommandContext ctx)
        {
            await TriviaServices.AskQuestion(ctx, ctx.Channel);
        }

        [Command("start")]
        [OwnerOrPermission(Permissions.KickMembers)]
        [Description("Starts a trivia game")]
        public async Task StartTriviaGame(CommandContext ctx, DiscordChannel channel)
        {
            var started = TriviaServices.StartCountdownTriviaTimer(ctx, channel);

            var message = started
                ? $"Trivia wil now start in {channel.Mention} in 10 seconds"
                : "Trivia could not be started, this could be due to a game already being run";

            await ctx.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }

        [Command("stop")]
        [OwnerOrPermission(Permissions.KickMembers)]
        [Description("Stops a trivia game")]
        public async Task StopTriviaGame(CommandContext ctx)
        {
            var stopped = TriviaServices.StopTrivia();

            var message = stopped ? "Trivia has been stopped" : "No trivia found to be stopped";

            await ctx.Channel.SendMessageAsync(message);
        }
    }
}
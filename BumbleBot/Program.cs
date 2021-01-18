namespace BumbleBot
{
    internal class MainClass
    {
        public static void Main(string[] args)
        {
            var bot = new Bot();
            bot.RunAsync().GetAwaiter().GetResult();
        }
    }
}
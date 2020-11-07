using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace BumbleBot.Commands.Game
{
    [Group("Milk")]
    [Hidden]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class MilkCommands : BaseCommandModule
    {
        private DBUtils dbUtils = new DBUtils();
        GoatService goatService { get; }

        public MilkCommands(GoatService goatService)
        {
            this.goatService = goatService;
        }

        [GroupCommand]
        public async Task MilkGoats(CommandContext ctx)
        {
            try
            {
                String uri = $"http://localhost:8080/milk/{ctx.User.Id}";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using(HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using(Stream stream = response.GetResponseStream())
                using(StreamReader reader = new StreamReader(stream))
                {
                    var jsonReader = new JsonTextReader(reader);
                    JsonSerializer serializer = new JsonSerializer();
                    MilkingResponse milkingResponse = serializer.Deserialize<MilkingResponse>(jsonReader);

                    await ctx.Channel.SendMessageAsync(milkingResponse.message).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
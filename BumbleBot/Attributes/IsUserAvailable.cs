using System;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MySql.Data.MySqlClient;

namespace BumbleBot.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class IsUserAvailable : CheckBaseAttribute
    {
        private readonly DbUtils dBUtils = new();
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "select working from farmers where DiscordId = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?userId", ctx.User.Id);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                       return Task.FromResult(!reader.GetBoolean("working")); 
                    }
                    
                }
                else
                {
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(true);
        }
    }
}
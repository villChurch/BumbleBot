using System.Threading.Tasks;
using BumbleBot.Utilities;
using DisCatSharp.ApplicationCommands;
using MySql.Data.MySqlClient;

namespace BumbleBot.Attributes
{
    public class IsUserAvailableSlash : SlashCheckBaseAttribute
    {
        private readonly DbUtils dBUtils = new();
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
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
using System;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using MySql.Data.MySqlClient;

namespace BumbleBot.Attributes
{
    public class HasEnoughCredits : CheckBaseAttribute
    {
        private readonly DbUtils dBUtils = new();

        public HasEnoughCredits(int argumentPosition)
        {
            this.ArgumentPosition = argumentPosition;
        }

        private int Balance { get; set; }
        private int ArgumentPosition { get; set; }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (help)
            {
                return Task.FromResult(true);
            }
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
            {
                var query = "select * from farmers where DiscordID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        Balance = reader.GetInt32("credits");
                else
                    Balance = 0;
                reader.Close();
            }

            var arguments = ctx.RawArgumentString.Split(' ');
            var buyPrice = Convert.ToInt32(arguments[ArgumentPosition]);
            return Task.FromResult(Balance >= buyPrice);
        }
    }
}
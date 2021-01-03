using System;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MySql.Data.MySqlClient;

namespace BumbleBot.Attributes
{
    public class HasEnoughCredits : CheckBaseAttribute
    {
        private int balance { get; set; }
        private DBUtils dBUtils = new DBUtils();
        public int argumentPosition { get; set; }
        public HasEnoughCredits(int argumentPosition)
        {
            this.argumentPosition = argumentPosition;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from farmers where DiscordID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        this.balance = reader.GetInt32("credits");
                    }
                }
                else
                {
                    balance = 0;
                }
                reader.Close();
            }

            var arguments = ctx.RawArgumentString.Split(' ');

            int buyPrice = Convert.ToInt32(arguments[argumentPosition]);
            return Task.FromResult(balance >= buyPrice);
        }
    }
}

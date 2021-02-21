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
        private readonly DbUtils dBUtils = new DbUtils();

        public HasEnoughCredits(int argumentPosition)
        {
            this.ArgumentPosition = argumentPosition;
        }

        private int Balance { get; set; }
        public int ArgumentPosition { get; set; }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
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
            if (ctx.Command.Name.ToLower().Equals("help")) return Task.FromResult(1 == 1);
            var buyPrice = Convert.ToInt32(arguments[ArgumentPosition]);
            return Task.FromResult(Balance >= buyPrice);
        }
    }
}
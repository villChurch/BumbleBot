﻿using System;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MySql.Data.MySqlClient;
using BumbleBot.Attributes;
using DSharpPlus;
using DSharpPlus.Interactivity;

namespace BumbleBot.Commands.AdminCommands
{
    [Group("config")]
    [Hidden]
    public class ConfigCommands : BaseCommandModule
    {
        private readonly DBUtils dbUtils = new DBUtils();
        [Command("asshole")]
        [Hidden]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task EnableAssholeMode(CommandContext ctx)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?boolValue", MySqlDbType.Int16).Value = 1;
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeMode";
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync("Parameter set").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("assholeresponse")]
        [Aliases("ar")]
        [Hidden]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task SetAssholeResponse(CommandContext ctx) {
            try
            {
                string currentResponse = "";
                using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select stringResponse from config where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeResponse";
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while(reader.Read())
                        {
                            currentResponse = reader.GetString("stringResponse");
                        }
                    }
                    reader.Close();
                }
                var interactivity = ctx.Client.GetInteractivity();
                await ctx.Channel.SendMessageAsync($"Current reponse is: {currentResponse} Please enter the new one").ConfigureAwait(false);
                var responseMsg = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel,
                    TimeSpan.FromMinutes(5));
                if (responseMsg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("This command has timed out").ConfigureAwait(false);
                }
                else
                {
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Update config set stringResponse = ?stringResponse where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?stringResponse", MySqlDbType.VarChar).Value = responseMsg.Result.Content;
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeResponse";
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    await ctx.Channel.SendMessageAsync("Response now updated").ConfigureAwait(false);
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

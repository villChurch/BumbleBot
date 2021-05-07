using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Text;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

namespace BumbleBot.Commands.MiscCommands
{
    public class Misc : BaseCommandModule
    {

        [Command("roll")]
        [Aliases("random")]
        [Description("Roll a random number between 0 and the number you enter")]
        public async Task RandomRoll(CommandContext ctx, [Description("Max value")] int max)
        {
            if (max <= 0)
            {
                await ctx.Channel.SendMessageAsync("Number must be greater than 0").ConfigureAwait(false);
            }
            else
            {
                Random rnd = new Random();
                var number = rnd.Next(0, max);
                await new DiscordMessageBuilder()
                    .WithContent($"{number}")
                    .WithReply(ctx.Message.Id, true)
                    .SendAsync(ctx.Channel)
                    .ConfigureAwait(false);
            }
        }

        [Group("stick")]
        public class Stick : BaseCommandModule
        {
            private DbUtils dbUtils = new DbUtils();
            [GroupCommand]
            [Description("Shake mr stick at someone")]
            [Cooldown(3, 60, CooldownBucketType.User)]
            public async Task ShakeMrStick(CommandContext ctx, DiscordMember member)
            {
                using (var con = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    const string query =
                        "Insert into sticked (recipientId, stickerId, messageLink) values (?recipientId, ?stickerId, ?messageLink)";
                    var command = new MySqlCommand(query, con);
                    command.Parameters.AddWithValue("?recipientId", member.Id);
                    command.Parameters.AddWithValue("?stickerId", ctx.Member.Id);
                    command.Parameters.AddWithValue("?messageLink", ctx.Message.JumpLink.ToString());
                    con.Open();
                    command.ExecuteNonQuery();
                    await con.CloseAsync();
                }
                var mrStick = DiscordEmoji.FromName(ctx.Client, ":mrstick:");
                await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention} shakes {mrStick} at {member.Mention}.")
                    .ConfigureAwait(false);
            }

            [Command("show")]
            [Aliases("list")]
            [Description("Show all the times you have been shown mr stick")]
            public async Task ShowTimesIHaveBeenSticked(CommandContext ctx)
            {
                List<Sticked> listOfSticks = new List<Sticked>();
                using (var con = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    const string query = "select * from sticked where recipientId = ?userId";
                    var command = new MySqlCommand(query, con);
                    command.Parameters.AddWithValue("?userId", ctx.User.Id);
                    con.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            listOfSticks.Add(new Sticked(reader));
                        }
                    }
                    reader.Close();
                    await con.CloseAsync();
                }

                if (listOfSticks.Count > 0)
                {
                    List<DiscordUser> stickersDiscordUsers = new List<DiscordUser>();
                    List<ulong> distinctUserId = listOfSticks.Select(x => x.stickerId).Distinct().ToList();
                    distinctUserId.ForEach(async (x) =>
                    {
                        var user = await ctx.Client.GetUserAsync(x);
                        stickersDiscordUsers.Add(user);
                    });
                    var sb = new StringBuilder();
                    stickersDiscordUsers.ForEach(x =>
                    {
                        sb.AppendLine($"Shown mr stick by {x.Username}");
                    });
                    var interactivity = ctx.Client.GetInteractivity();
                    var stickPages =
                        interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, stickPages)
                        .ConfigureAwait(false));
                }
            }
            
            [Command("victims")]
            [Description("Show all the victims you have shown mr stick to")]
            public async Task ShowVictimsIHaveSticked(CommandContext ctx)
            {
                List<Sticked> listOfSticks = new List<Sticked>();
                using (var con = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    const string query = "select * from sticked where stickerId = ?userId";
                    var command = new MySqlCommand(query, con);
                    command.Parameters.AddWithValue("?userId", ctx.User.Id);
                    con.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            listOfSticks.Add(new Sticked(reader));
                        }
                    }
                    reader.Close();
                    await con.CloseAsync();
                }

                if (listOfSticks.Count > 0)
                {
                    List<DiscordUser> stickersDiscordUsers = new List<DiscordUser>();
                    List<ulong> distinctUserId = listOfSticks.Select(x => x.stickerId).Distinct().ToList();
                    distinctUserId.ForEach(async (x) =>
                    {
                        var user = await ctx.Client.GetUserAsync(x);
                        stickersDiscordUsers.Add(user);
                    });
                    var sb = new StringBuilder();
                    stickersDiscordUsers.ForEach(x =>
                    {
                        sb.AppendLine($"You showed mr stick to {x.Username}");
                    });
                    var interactivity = ctx.Client.GetInteractivity();
                    var stickPages =
                        interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, stickPages)
                        .ConfigureAwait(false));
                }
            }
        }
    }
}
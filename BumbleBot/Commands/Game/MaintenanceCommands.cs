using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("maintenance")]
    [IsUserAvailable]
    public class MaintenanceCommands : BaseCommandModule
    {

        private MaintenanceService MaintenanceService { get; }
        private FarmerService FarmerService { get; }
        public MaintenanceCommands(MaintenanceService maintenanceService, FarmerService farmerService)
        {
            MaintenanceService = maintenanceService;
            FarmerService = farmerService;
        }

        [GroupCommand]
        [Description("See your maintenance information")]
        public async Task CheckIfMaintenanceIsNeeded(CommandContext ctx)
        {
            var farmersMaintenance = MaintenanceService.GetFarmersMaintenance(ctx.User.Id);
            var maintenanceCost = MaintenanceService.GetMaintenanceRepairCost(ctx.User.Id);

            var embed = new DiscordEmbedBuilder()
            {
                Title = $"{((DiscordMember) ctx.User).DisplayName}'s Maintenance Report",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField("Maintenance Status",
                farmersMaintenance.needsMaintenance
                    ? $"Maintenance is needed and will cost {maintenanceCost} credits."
                    : "Maintenance is not needed at the moment.");

            embed.AddField("Milking Boost", farmersMaintenance.milkingBoost
                ? "The next time you milk you will get a 10% boost because of good maintenance."
                : "There is currently no milking boost from maintenance.");

            embed.AddField("Daily Boost", farmersMaintenance.dailyBoost
                ? "The next time you run daily you will get a 10% boost because of good maintenance."
                : "There is currently no daily boost from maintenance.");

            await ctx.Channel.SendMessageAsync(embed).ConfigureAwait(false);
        }

        [HasEnoughCredits(0)]
        [Command("pay")]
        [Description("Pay maintenance fees")]
        public async Task PayMaintenanceFee(CommandContext ctx, int cost)
        {
            var maintenanceCost = MaintenanceService.GetMaintenanceRepairCost(ctx.User.Id);
            if (cost != maintenanceCost)
            {
                await ctx.Channel
                    .SendMessageAsync(
                        $"Your maintenance cost is {maintenanceCost} credits not {cost} credits. This may have increased due to gaining more goats.")
                    .ConfigureAwait(false);
            }
            else
            {
                FarmerService.DeductCreditsFromFarmer(ctx.User.Id, maintenanceCost);
                MaintenanceService.SetMaintenanceAsCompleted(ctx.User.Id);
                await ctx.Channel
                    .SendMessageAsync(
                        $"Maintenance cost of {maintenanceCost} credits has been paid and milk production will no longer be decreased.")
                    .ConfigureAwait(false);
            }
        }
    }
}

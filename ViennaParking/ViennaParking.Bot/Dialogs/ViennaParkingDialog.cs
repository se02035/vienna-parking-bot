using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;
using ViennaParking.Bot.Dialogs;
using ViennaParking.Bot.Helper;
using System.Collections.Concurrent;
using System.Linq;

namespace ViennaParking.Bot.Dialogs
{
    [Serializable]
    public enum ParkingZoneOptions
    {
        Unspecified = 0,
        CheckZone,
        FindShops,
        BuyTicket,
        Exit
    }

    [Serializable]
    public class ViennaParkingDialog : IDialog<string>
    {
        public readonly IDialog<Message> Antecedent;
        public ParkingZoneOptions CurrentAction;
        public UserLocation Location;
        public ViennaParkingDialog(IDialog<Message> antecedent)
        {
            SetField.NotNull(out Antecedent, nameof(antecedent), antecedent);
        }

        public static IDialog<string> BotDialog = Chain
            .PostToChain()
            .GetViennaParking()
            .PostToUser();

        public async Task StartAsync(IDialogContext context)
        {
            context.Call(Antecedent, ResumeAsync);
        }

        private async Task ResumeAsync(IDialogContext context, IAwaitable<Message> result)
        {
            var item = await result;

            await context.PostAsync("Hi. I'm the Vienna parking bot.");

            RequestUserAction(context, "How can I help you?");
        }

        private void RequestUserAction(IDialogContext context, string prompt)
        {
            PromptDialog.Choice(
                context,
                ResumeAfterUserSelection,
                new[] { ParkingZoneOptions.CheckZone, ParkingZoneOptions.FindShops, ParkingZoneOptions.BuyTicket, ParkingZoneOptions.Exit },
                prompt);
        }

        private async Task ResumeAfterUserSelection(IDialogContext context, IAwaitable<ParkingZoneOptions> result)
        {
            CurrentAction = await result; 
            await ExecuteAction(context, CurrentAction);
        }

        private async Task ExecuteAction(IDialogContext context, ParkingZoneOptions action)
        {
            var dlg = Chain.Return(Location);
            if (Location == null)
            {
                // if no location was specified so far then 
                // ask the user to provide details
                // otherwise re-use an already provided one
                dlg = dlg.GetUserLocation()
                    .Select((result) =>
                    {
                        Location = result;
                        return Location;
                    });
            }

            switch (action)
            {
                case ParkingZoneOptions.CheckZone:
                    context.Call(dlg.CheckParkingZone(), ResumeAfterParkingZoneCheck);
                    break;
                case ParkingZoneOptions.FindShops:
                    context.Call(dlg.FindTicketShops(), ResumeAfterTicketShopsSearch);
                    break;
                case ParkingZoneOptions.BuyTicket:
                    context.Call(dlg.BuyParkingTicket(), ResumeAfterTicketBought);
                    break;
                default:
                    // exit
                    Location = null;
                    context.Done("Hope to see you soon again!");
                    break;
            }
        }

        private async Task ResumeAfterTicketBought(IDialogContext context, IAwaitable<BuyTicketResult> result)
        {
            var buyRequest = await result;
            await context.PostAsync("Please send this text via SMS to your mobile ticket provider (e.g. 'Handy Parken')");
            await context.PostAsync($"{buyRequest.ParkingDurationInMin} {buyRequest.DestinationCity}*{buyRequest.LicensePlate}");
            RequestUserAction(context, "Can I help you with something else?");
        }

        private async Task ResumeAfterTicketShopsSearch(IDialogContext context, IAwaitable<TicketShopSearchResult> result)
        {
            var parkingTicketShops = await result;

            string responseMessage = "";
            if (parkingTicketShops != null &&
                parkingTicketShops.ParkingTicketShops != null &&
                parkingTicketShops.ParkingTicketShops.Any())
            {
                var formattedOutput =
                    $"I found **{parkingTicketShops.ParkingTicketShops.Count()}** ticket shops. Here are the closest ones: {Environment.NewLine}";

                foreach (var shop in parkingTicketShops.ParkingTicketShops)
                {
                    formattedOutput += 
                       $"* {shop.Address} (Distance: {shop.Distance}m)" +
                       $"{Environment.NewLine}";

                }
                responseMessage = formattedOutput;
            }
            else
            {
                responseMessage = "Sorry, but I couldn't find any parking ticket shops.";
            }

            await context.PostAsync(responseMessage);
            RequestUserAction(context, "Can I help you with something else?");
        }

        private async Task ResumeAfterParkingZoneCheck(IDialogContext context, IAwaitable<ShortParkingSearchResult> result)
        {
            var shortParkingZones = await result;

            string responseMessage = "";
            if (shortParkingZones != null &&
                shortParkingZones.ShortParkingZones != null &&
                shortParkingZones.ShortParkingZones.Any())
            {
                var formattedOutput =
                    $"The address is within a short parking area. Here are the details:";

                foreach (var zone in shortParkingZones.ShortParkingZones)
                {
                    formattedOutput += $"{Environment.NewLine}{Environment.NewLine}" +
                        $"* District: {zone.District} {Environment.NewLine}" +
                        $"* Period: {zone.Period} {Environment.NewLine}" +
                        $"* Duration: {zone.Duration} {Environment.NewLine}";

                }
                responseMessage = formattedOutput;
            }
            else
            {
                responseMessage = "The address you specified is not within a short parking area.";
            }

            await context.PostAsync(responseMessage);
            RequestUserAction(context, "Can I help you with something else?");
        }

    }
}
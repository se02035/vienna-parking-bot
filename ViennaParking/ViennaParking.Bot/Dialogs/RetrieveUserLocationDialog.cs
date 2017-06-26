using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;
using ViennaParking.Bot.Helper;

namespace ViennaParking.Bot.Dialogs
{
    [Serializable]
    public class UserLocation
    {
        public UserLocation() { }
        public UserLocation(double longitude, double latitude, string name, string city)
        {
            Longitude = longitude;
            Latitude = latitude;
            Name = name;
            City = city;
        }

        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
    }

    [Serializable]
    public class RetrieveUserLocationDialog<R> : IDialog<UserLocation>
    {
        private const string UnknownAddress = "Unknown Address";

        public readonly IDialog<R> Antecedent;
        public UserLocation CurrentUserLocation;

        public RetrieveUserLocationDialog(
            IDialog<R> antecedent)
        {
            SetField.NotNull(out Antecedent, nameof(antecedent), antecedent);
        }

        public RetrieveUserLocationDialog() { }

        public async Task StartAsync(IDialogContext context)
        {
            context.Call(Antecedent, ResumeAsync);
        }

        private async Task ResumeAsync(IDialogContext context, IAwaitable<R> result)
        {
            var rawUserMessage = await result;

            await RetrieveLocation(context, "Please, provide your location?");
        }

        internal async Task RetrieveLocation(IDialogContext context, string message)
        {
            await context.PostAsync(message);
            context.Wait<Message>(LocationDataReceived);
        }

        private async Task LocationDataReceived(IDialogContext context, IAwaitable<Message> result)
        {
            var rawLocationMessage = await result;

            UserLocation location = null;
            if (rawLocationMessage.Location != null)
            {
                location = new UserLocation(
                    rawLocationMessage.Location.Longitude.GetValueOrDefault(),
                    rawLocationMessage.Location.Latitude.GetValueOrDefault(),
                    rawLocationMessage.Location.Name,
                    string.Empty);

                if (String.IsNullOrWhiteSpace(location.Name))
                {
                    var verifiedAddress =
                        GeoHelper.GetAddress(location.Longitude, location.Latitude)
                                        .FirstOrDefault();
                    if (verifiedAddress != null)
                    {
                        location.Name = verifiedAddress.FormattedAddress;
                        location.City = verifiedAddress.Locality;
                    }
                    else
                    {
                        location.Name = UnknownAddress;
                    }

                }

            }
            else if (!string.IsNullOrEmpty(rawLocationMessage.Text))
            {
                var retrievedAddress = GeoHelper.Lookup(rawLocationMessage.Text).FirstOrDefault();
                if (retrievedAddress != null)
                {
                    location = new UserLocation(
                        retrievedAddress.Coordinates.Longitude,
                        retrievedAddress.Coordinates.Latitude,
                        retrievedAddress.FormattedAddress,
                        retrievedAddress.Locality);
                }
            }

            CurrentUserLocation = location;
            if (CurrentUserLocation == null)
            {
                // address not found - retry 
                await RetrieveLocation(context, "Sorry, but I couldn't find an address. Please try again but be more specific. What's your address?");
            }
            else
            {
                PromptDialog.Confirm(
                    context,
                    ResumeAfterLocationConfirmed,
                    $"I found this address: {location.Name}. Is this correct?");
            }

        }

        private async Task ResumeAfterLocationConfirmed(IDialogContext context, IAwaitable<bool> result)
        {
            var addressConfirmed = await result;

            if (addressConfirmed)
                context.Done(CurrentUserLocation);
            else
                await RetrieveLocation(context, "Ok, no problem. Please try again. What's your address?");
        }
    }
}
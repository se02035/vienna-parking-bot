using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Linq;
using ViennaParking.Bot.Helper;
using ViennaParking.Data;
using Microsoft.Bot.Connector;

namespace ViennaParking.Bot
{
    [Serializable]
    [Template(TemplateUsage.NotUnderstood, "I do not understand \"{0}\".", "Try again, I don't get \"{0}\".")]
    public class ParkingZoneCheck
    {
        [Prompt("What's your {&}")]
        public string Address { get; set; }
        public VerifiedAddress VerifiedAddress { get; set; }
        public Location MyLocation { get; set; }

        public IForm<ParkingZoneCheck> BuildForm()
        {
            OnCompletionAsyncDelegate<ParkingZoneCheck> processOrder = async (context, state) =>
            {
                // check if the address is within a short parking zone
                var foundParkingZone = DataManager.GetParkingZone(
                                    state.VerifiedAddress.Longitude, 
                                    state.VerifiedAddress.Latitude);

                string responseMessage; ;
                if (foundParkingZone != null)
                {
                    //responseMessage =
                    //    $"The address is within a short parking area. Here are the details:" +
                    //    $"{Environment.NewLine}{Environment.NewLine}" +
                    //    $"* Period: {foundParkingZone.Period} {Environment.NewLine}" +
                    //    $"* Duration: {foundParkingZone.Duration} {Environment.NewLine}";
                }
                else
                {
                    responseMessage = "You are currently not in a short parking area.";
                }

                // Actually process the ticket shop search
                //await context.PostAsync(responseMessage);
            };

            return new FormBuilder<ParkingZoneCheck>()
                .Message("Welcome to the parking zone validation bot")
                .Field(nameof(Address),
                    validate: async (state, response) =>
                    {
                        var result = new ValidateResult { IsValid = true };
                        var retrievedAddress = (await GeoHelper.LookupAsync(response as string)).FirstOrDefault();
                        if (retrievedAddress != null)
                        {
                            state.VerifiedAddress = new VerifiedAddress()
                            {
                                Address = retrievedAddress.FormattedAddress,
                                Latitude = retrievedAddress.Coordinates.Latitude,
                                Longitude = retrievedAddress.Coordinates.Longitude
                            };
                        }
                        else
                        {
                            result.Feedback = "Sorry, but I couldn't find this address. Can you please be more precise and try again?";
                            result.IsValid = false;
                        }

                        return result;
                    })
                .Confirm(async (state) =>
                {
                    return new PromptAttribute($"I think I found the address '{state.VerifiedAddress.Address}'. Is this correct?");
                })
                .Message("Thanks! Start parking zone validation")
                .OnCompletionAsync(processOrder)
                .Build();
        }
    }
}
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Linq;
using ViennaParking.Data;
using ViennaParking.Bot.Helper;

namespace ViennaParking.Bot
{
    [Serializable]
    [Template(TemplateUsage.NotUnderstood, "I do not understand \"{0}\".", "Try again, I don't get \"{0}\".")]
    public class ShopSearch
    {
        [Prompt("What's your {&}")]
        public string Address { get; set; }
        public VerifiedAddress VerifiedAddress { get; set; }

        public IForm<ShopSearch> BuildForm()
        {
            OnCompletionAsyncDelegate<ShopSearch> processOrder = async (context, state) =>
            {
                // find for shops close by
                var foundShops = DataManager.GetTicketShops(
                                    state.VerifiedAddress.Longitude,
                                    state.VerifiedAddress.Latitude);

                context.UserData.SetValue("verifiedaddress", state.VerifiedAddress);

                // create response message
                var responseMessage =
                    $"I found **{foundShops.Count()}** ticket shops. Here are the closest ones" +
                    $"{Environment.NewLine}{Environment.NewLine}";
                foreach (var shop in foundShops)
                {
                    responseMessage +=
                        $"* {shop.Address} (Distance: {shop.Distance}m)" +
                        $"{Environment.NewLine}";
                }

                // Actually process the ticket shop search
                await context.PostAsync(responseMessage);
            };

            return new FormBuilder<ShopSearch>()
                .Message("Welcome to the parking bot")
                .Field(nameof(Address),
                    active: (state) =>
                    {
                        if (VerifiedAddress != null)
                        {
                            state.VerifiedAddress = VerifiedAddress;
                            
                        }
                        return state.VerifiedAddress == null;
                    },
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
                .Message("Thanks! Start searching for ticket shops")
                .OnCompletionAsync(processOrder)
                .Build();
        }
    }
}
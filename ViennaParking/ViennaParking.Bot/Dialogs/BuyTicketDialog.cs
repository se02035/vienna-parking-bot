using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.FormFlow;

namespace ViennaParking.Bot.Dialogs
{
    [Serializable]
    public class BuyTicketResult
    {
        public UserLocation Location { get; set; }
        public string LicensePlate { get; set; }
        public int ParkingDurationInMin { get; set; }
        public string DestinationCity { get; set; }
    }

    [Serializable]
    public class BuyTicketDialog : IDialog<BuyTicketResult>
    {
        public enum ParkingDurationEnum 
        {
            [Describe("15 min")]
            Min15 = 0,
            [Describe("30 min")]
            Min30,
            [Describe("60 min")]
            Min60,
            [Describe("120 min")]
            Min120
        }

        private const string UserDataKeyLicensePlate = "licenseplate";
        private const string UserDataKeyParkDuration = "parkduration";

        public readonly string Greeting;
        public readonly IDialog<UserLocation> Ancestor;

        public UserLocation UserLocation;
        public string LicensePlate;
        public ParkingDurationEnum ParkDuration;
        public string DestinationCity;

        public BuyTicketDialog(IDialog<UserLocation> ancestor, string greeting)
        {
            SetField.NotNull(out Ancestor, nameof(ancestor), ancestor);
            SetField.NotNull(out this.Greeting, nameof(greeting), greeting);
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Call(Ancestor, ResumeAsync);
        }

        private async Task ResumeAsync(IDialogContext context, IAwaitable<UserLocation> result)
        {
            UserLocation = await result;

            await context.PostAsync(Greeting);
            ValidateDestinationCity(context, UserLocation.City);
        }

        private async Task RetrieveLicensePlate(IDialogContext context, string message)
        {
            await context.PostAsync(message);
            context.Wait(ResumeLicensePlateReceived);
        }

        private async Task ResumeLicensePlateReceived(IDialogContext context, IAwaitable<Message> result)
        {
            var rawUserMessage = await result;
            var licensePlate = rawUserMessage.Text;

            if (IsValidLicensePlate(licensePlate))
            {
                LicensePlate = licensePlate;

                context.Done(new BuyTicketResult()
                {
                    DestinationCity = ConvertDesintationCity(DestinationCity),
                    LicensePlate = LicensePlate,
                    Location = UserLocation,
                    ParkingDurationInMin = int.Parse(ParkDuration.ToString().Remove(0,3))
                });
            }
            else
            {
                await RetrieveLicensePlate(context, "Sorry. The license plate is invalid. Please try again using a valid one");
            }
        }

        private async Task ResumeDurationDurationSelected(IDialogContext context, IAwaitable<ParkingDurationEnum> result)
        {
            var selectedParkDuration = await result;
            ParkDuration = selectedParkDuration;

            // set the license plate
            await RetrieveLicensePlate(context, "What's your license plate");
        }

        private void ValidateDestinationCity(IDialogContext context, string destinationCity)
        {
            DestinationCity = destinationCity ?? string.Empty;

            PromptDialog.Confirm(
                context,
                ResumeAfterDestinationCityConfirmation, $"Do you want to buy a parking ticket for '{destinationCity}'?");
        }

        private async Task ResumeAfterDestinationCityConfirmation(IDialogContext context, IAwaitable<bool> result)
        {
            var isDestinationCityCorrect = await result;
            if (isDestinationCityCorrect)
            {
                PromptDialog.Choice(
                    context,
                    ResumeDurationDurationSelected,
                    new[] {
                                        ParkingDurationEnum.Min15,
                                        ParkingDurationEnum.Min30,
                                        ParkingDurationEnum.Min60,
                                        ParkingDurationEnum.Min120 },
                    $"Great! How long to you plan to park in {DestinationCity}?");
            }
            else
            {
                await context.PostAsync("Ok, let's change this. For which city do you need a parking ticket?");
                context.Wait(async (ctx, rawUserMessage) =>
                {
                    var userProvidedCityMsg = await rawUserMessage;
                    ValidateDestinationCity(ctx, userProvidedCityMsg.Text);
                });
            }
        }

        private string ConvertDesintationCity(string userInput)
        {
            string result;
            switch (userInput.ToLower())
            {
                case "vienna": result = "Wien"; break;
                //ToDo other cities
                default: result = userInput; break;
            }
            return result;
        }

        private bool IsValidLicensePlate(string licensePlate)
        {
            var result = true;

            if (String.IsNullOrWhiteSpace(licensePlate))
            {
                return false;
            }

            return result;
        }
    }
}
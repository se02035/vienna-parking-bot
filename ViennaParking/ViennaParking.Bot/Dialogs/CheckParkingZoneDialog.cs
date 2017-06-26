using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Internals.Fibers;
using ViennaParking.Data;
using System.Collections.Generic;

namespace ViennaParking.Bot.Dialogs
{

    [Serializable]
    public class ShortParkingSearchResult
    {
        public UserLocation Location { get; set; }
        public IEnumerable<ShortParkingZone> ShortParkingZones { get; set; }
    }

    [Serializable]
    public class CheckParkingZoneDialog : IDialog<ShortParkingSearchResult>
    {
        public readonly IDialog<UserLocation> Antecedent;

        public UserLocation Location;

        public CheckParkingZoneDialog(IDialog<UserLocation> antecedent)
        {
            SetField.NotNull(out Antecedent, nameof(antecedent), antecedent);
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Call(Antecedent, ResumeAsync);
        }

        private async Task ResumeAsync(IDialogContext context, IAwaitable<UserLocation> result)
        {
            Location = await result;

            // Step 1: Get information whether this is a short parking zone
            var zoneInformation = GetParkingZoneInformation(Location);
            context.Done(zoneInformation);
        }

        private ShortParkingSearchResult GetParkingZoneInformation(UserLocation userLocation)
        {
            var foundParkingZone = DataManager.GetParkingZone(
                userLocation.Longitude,
                userLocation.Latitude);

            List<ShortParkingZone> searchResult = new List<ShortParkingZone>();
            foreach (var zone in foundParkingZone)
            {
                searchResult.Add(new ShortParkingZone()
                {
                    District = zone.District,
                    Duration = zone.Duration,
                    Period = zone.Period
                });
            }

            return new ShortParkingSearchResult() {
                Location = this.Location,
                ShortParkingZones = searchResult
            };
        }
    }
}
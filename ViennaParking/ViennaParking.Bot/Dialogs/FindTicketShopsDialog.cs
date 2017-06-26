using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Internals.Fibers;
using ViennaParking.Data;
using System.Collections.Generic;

namespace ViennaParking.Bot.Dialogs
{


    [Serializable]
    public class TicketShopSearchResult
    {
        public UserLocation Location { get; set; }
        public IEnumerable<TicketShop> ParkingTicketShops { get; set; }
    }

    [Serializable]
    public class FindTicketShopsDialog : IDialog<TicketShopSearchResult>
    {
        public readonly IDialog<UserLocation> Ancestor;

        public UserLocation Location;

        public FindTicketShopsDialog(IDialog<UserLocation> ancestor)
        {
            SetField.NotNull(out Ancestor, nameof(ancestor), ancestor);
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Call(this.Ancestor, ResumeAsync);
        }

        private async Task ResumeAsync(IDialogContext context, IAwaitable<UserLocation> result)
        {
            Location = await result;
            
            // find for shops close by
            var foundShops = DataManager.GetTicketShops(
                    Location.Longitude,
                    Location.Latitude);

            List<TicketShop> shops = new List<TicketShop>();
            foreach (var shop in foundShops)
            {
                
                shops.Add(new TicketShop()
                {
                    Address = shop.Address,
                    Distance = shop.Distance
                });
            }

            context.Done(new TicketShopSearchResult() {
                Location = this.Location,
                ParkingTicketShops = shops
            });
        }
    }
}
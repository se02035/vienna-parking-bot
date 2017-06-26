using Microsoft.Bot.Connector;
using System;
using Microsoft.Bot.Builder.Dialogs;
using ViennaParking.Bot.Dialogs;

namespace ViennaParking.Bot.Helper
{
    [Serializable]
    public static class DialogHelper
    {
        public static IDialog<UserLocation> GetUserLocation<T>(this IDialog<T> antecedent)
        {
            return new RetrieveUserLocationDialog<T>(antecedent);
        }

        public static IDialog<TicketShopSearchResult> FindTicketShops(this IDialog<UserLocation> antecedent)
        {
            return new FindTicketShopsDialog(antecedent);
        }

        public static IDialog<BuyTicketResult> BuyParkingTicket(this IDialog<UserLocation> antecedent)
        {
            return new BuyTicketDialog(antecedent, "I need a few more details");
        }

        public static IDialog<ShortParkingSearchResult> CheckParkingZone(this IDialog<UserLocation> antecedent)
        {
            return new CheckParkingZoneDialog(antecedent);
        }

        public static IDialog<string> GetViennaParking(this IDialog<Message> antecedent)
        {
            return new Dialogs.ViennaParkingDialog(antecedent);
        }
    }
}
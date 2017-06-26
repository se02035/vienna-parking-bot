using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViennaParking.Data.ViewModels
{
    public class TicketShopViewModel
    {
        public string Address { get; set; }
        public double Distance { get; set; }
        public string Id { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}

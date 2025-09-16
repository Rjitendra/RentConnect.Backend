using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentConnect.Models.Dtos.Stripe_Payments
{
    public class AccountLinkResponse
    {
        public string Object { get; set; }
        public DateTime Created { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Url { get; set; }
    }
}

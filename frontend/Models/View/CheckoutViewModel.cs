using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Models.View
{
    public class CheckoutViewModel
    {
        public Guid BasketId { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Address { get; set; } = String.Empty;
        public string Town { get; set; } = String.Empty;
        public string PostalCode { get; set; } = String.Empty;
        [EmailAddress]
        public string Email { get; set; } = String.Empty;
        [CreditCard]
        public string CreditCard { get; set; } = String.Empty;
        public string CreditCardDate { get; set; } = String.Empty;
    }
}

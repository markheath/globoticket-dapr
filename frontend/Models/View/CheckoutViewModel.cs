using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Models.View
{
    public class CheckoutViewModel
    {
        public Guid BasketId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Town { get; set; }
        public string PostalCode { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [CreditCard]
        public string CreditCard { get; set; }
        public string CreditCardDate { get; set; }
    }
}

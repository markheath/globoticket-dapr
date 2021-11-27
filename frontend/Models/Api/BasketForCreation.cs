using System;
using System.ComponentModel.DataAnnotations;

namespace GloboTicket.Frontend.Models.Api
{
    public class BasketForCreation
    {
        [Required]
        public Guid UserId { get; set; }
    }
}

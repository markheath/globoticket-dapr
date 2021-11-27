using System;
using System.Collections.Generic;
using GloboTicket.Frontend.Models.Api;

namespace GloboTicket.Frontend.Models.View
{
    public class EventListModel
    {
        public IEnumerable<Event> Events { get; set; }
        public int NumberOfItems { get; set; }
    }
}

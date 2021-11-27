using GloboTicket.Frontend.Models.View;

namespace GloboTicket.Frontend.Services.Ordering
{
    public interface IOrderSubmissionService
    {
        Task<Guid> SubmitOrder(CheckoutViewModel checkoutViewModel);
    }
}

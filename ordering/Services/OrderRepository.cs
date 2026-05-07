using System.Text.Json;
using Dapr.Client;
using GloboTicket.Ordering.Model;

namespace GloboTicket.Ordering.Services;

public class OrderRepository
{
    private const string StateStoreName = "orderstore";

    private readonly DaprClient daprClient;

    public OrderRepository(DaprClient daprClient)
    {
        this.daprClient = daprClient;
    }

    // Persists the order to the Dapr state store. Uses ExecuteStateTransactionAsync
    // (not SaveStateAsync) because the outbox publishes only when state is written
    // through the transactional API. The contentType is what Dapr propagates to
    // the outbox CloudEvent's datacontenttype — without it, the subscribing
    // controller rejects the body with 415 Unsupported Media Type.
    public Task SaveAsync(OrderForCreation order)
    {
        var operation = new StateTransactionRequest(
            key: $"order-{order.OrderId}",
            value: JsonSerializer.SerializeToUtf8Bytes(order),
            operationType: StateOperationType.Upsert,
            metadata: new Dictionary<string, string>
            {
                ["contentType"] = "application/json"
            });

        return daprClient.ExecuteStateTransactionAsync(StateStoreName, [operation]);
    }
}

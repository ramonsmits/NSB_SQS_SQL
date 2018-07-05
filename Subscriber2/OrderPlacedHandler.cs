using System.Threading.Tasks;
using NServiceBus;
using System;
using Shared.Events;

public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}

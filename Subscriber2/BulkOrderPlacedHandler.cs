using System.Threading.Tasks;
using NServiceBus;
using System;
using Shared.Events;

public class BulkOrderPlacedHandler : IHandleMessages<BulkOrderPlaced>
{
    public Task Handle(BulkOrderPlaced message, IMessageHandlerContext context)
    {
        Console.WriteLine("Handled by subscriber 2: BulkOrderPlaced for Order Id: "+message.OrderId);
        return Task.CompletedTask;
    }
}

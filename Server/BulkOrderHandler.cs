using System.Threading.Tasks;
using NServiceBus;
using System;
using Shared.Command;

public class BulkOrderHandler :
    IHandleMessages<BulkOrder>
{
    public Task Handle(BulkOrder message, IMessageHandlerContext context)
    {
        Console.WriteLine("Bulk Order placed with id: " + message.Id);
        Console.WriteLine("Publishing: BulkOrderPlaced for Order Id: " + message.Id);
        return Task.CompletedTask;
    }
}
using System.Threading.Tasks;
using NServiceBus;
using System;
using Shared.Command;

public class BulkOrderHandler :
    IHandleMessages<BulkOrder>
{
    public Task Handle(BulkOrder message, IMessageHandlerContext context)
    {
        Console.Out.WriteLineAsync("Bulk Order placed with id: " + message.Id);
        return Task.CompletedTask;
    }
}
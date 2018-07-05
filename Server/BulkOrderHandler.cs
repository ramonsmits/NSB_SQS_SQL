using System.Threading.Tasks;
using NServiceBus;
using System;
using Shared.Command;

public class BulkOrderHandler :
    IHandleMessages<BulkOrder>
{
    public Task Handle(BulkOrder message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}
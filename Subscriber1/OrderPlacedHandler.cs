using System.Threading.Tasks;
using NServiceBus;
using System;
using Shared.Events;

public class OrderPlacedHandler : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        Console.Out.WriteLineAsync("Handled by subscriber 1: OrderPlaced for Order Id: " + message.OrderId);
        return Task.CompletedTask;
    }
}
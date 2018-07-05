using System.Threading.Tasks;
using NServiceBus;
using System;
using Shared.Command;
using Shared.Events;

public class PlaceOrderHandler :
    IHandleMessages<PlaceOrder>
{
    public Task Handle(PlaceOrder message, IMessageHandlerContext context)
    {
        Console.Out.WriteLineAsync("Order for Product:" + message.Product + " placed with id: " + message.Id);

        var orderPlaced = new OrderPlaced
        {
            OrderId = message.Id
        };

        return context.Publish(orderPlaced);
    }
}
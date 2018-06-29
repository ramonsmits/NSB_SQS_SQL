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
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Order for Product:" + message.Product + " placed with id: " + message.Id);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Publishing: OrderPlaced for Order Id: " + message.Id);

        var orderPlaced = new OrderPlaced
        {
            OrderId = message.Id
        };

        return context.Publish(orderPlaced);
    }
}
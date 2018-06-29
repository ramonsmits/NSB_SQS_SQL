using System;
using NServiceBus;

namespace Shared.Command
{
    public class PlaceOrder :
        ICommand
    {
        public Guid Id { get; set; }
        public string Product { get; set; }
    }
}
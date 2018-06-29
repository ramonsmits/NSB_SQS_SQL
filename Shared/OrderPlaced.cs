using System;
using NServiceBus;

namespace Shared.Events
{
    public class OrderPlaced : IEvent
    {
        public Guid OrderId { get; set; }
    }
}
using System;
using NServiceBus;

namespace Shared.Command
{
    public class BulkOrder :
        ICommand
    {
        public Guid Id { get; set; }
        public byte[] Data { get; set; }
    }
}
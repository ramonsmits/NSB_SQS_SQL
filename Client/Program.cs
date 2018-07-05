using Infrastructure;
using Shared.Command;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static SendAndProcessEndpoint<BaseEndpointConfig> _endpoint;
        static void Main()
        {
            _endpoint = new SendAndProcessEndpoint<BaseEndpointConfig>(new ClientConfig());
            AsyncMain().GetAwaiter().GetResult();
        }

        static async Task AsyncMain()
        {
            Console.Title = "NSB.Client";
            _endpoint.Initialize();
            await _endpoint.StartEndpoint()
                .ConfigureAwait(false);

            try
            {
                await SendOrder()
                    .ConfigureAwait(false);
            }
            finally
            {
                _endpoint.StopEndpoint();
            }
        }

        static async Task SendOrder()
        {
            var tasks = new List<Task>();
            Console.ForegroundColor = ConsoleColor.White;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Press enter number of messages to be sent");
                Console.ForegroundColor = ConsoleColor.White;
                var number = Convert.ToInt32(Console.ReadLine());
                int a = 0;

                while (a < number)
                {
                    var id = Guid.NewGuid();
                    var placeOrder = new PlaceOrder
                    {
                        Product = "New shoes",
                        Id = id
                    };

                    tasks.Add(_endpoint.SendMessage(placeOrder));


                    a++;
                }

                await Task.WhenAll(tasks)
                    .ConfigureAwait(false);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Sending complete!");
            }
        }
    }
}

using Infrastructure;
using System;
using System.Threading.Tasks;

namespace Subscriber2
{
    class Program
    {
        static SendAndProcessEndpoint<BaseEndpointConfig> _endpoint;
        static void Main()
        {
            _endpoint = new SendAndProcessEndpoint<BaseEndpointConfig>(new Subscriber2Config());
            AsyncMain().GetAwaiter().GetResult();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private static async Task AsyncMain()
        {
            _endpoint.Initialize();
            Console.Title = "NSB.Subscriber2";

            await _endpoint.StartEndpoint()
                .ConfigureAwait(false);
            Console.ReadKey();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            _endpoint.StopEndpoint();
        }
    }
}

using System;
using System.Threading.Tasks;
using Infrastructure;

namespace Server
{
    class Program
    {
        static SendAndProcessEndpoint<BaseEndpointConfig> _endpoint;
        static void Main()
        {
            _endpoint = new SendAndProcessEndpoint<BaseEndpointConfig>(new ServerConfig());
            AsyncMain().GetAwaiter().GetResult();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        static async Task AsyncMain()
        {
            _endpoint.Initialize();
            Console.Title = "NSB.Server";
           
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

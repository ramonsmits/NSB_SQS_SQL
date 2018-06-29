using System;
using System.Configuration;
using System.Threading.Tasks;
using Infrastructure;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Transport.SQLServer;
using Shared.Events;
using Configuration = NHibernate.Cfg.Configuration;
using Environment = NHibernate.Cfg.Environment;

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

        static async Task AsyncMain1()
        {
            Console.Title = "Samples.PubSub.Publisher";
            var endpointConfiguration = new EndpointConfiguration("Samples.PubSub.Publisher");
            //endpointConfiguration.UsePersistence<LearningPersistence>();
            //endpointConfiguration.UseTransport<LearningTransport>();

            //Persistence
            var persistence = endpointConfiguration.UsePersistence<NHibernatePersistence>();
            var nhConfig = new Configuration();
            nhConfig.SetProperty(Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider");
            nhConfig.SetProperty(Environment.ConnectionDriver, "NHibernate.Driver.Sql2008ClientDriver");
            nhConfig.SetProperty(Environment.Dialect, "NHibernate.Dialect.MsSql2008Dialect");
            nhConfig.SetProperty(Environment.ConnectionString, ConfigurationManager.ConnectionStrings["NSB_AWS.NHibernatePersistence"].ConnectionString);
            nhConfig.SetProperty(Environment.DefaultSchema, "nsb");
            persistence.UseConfiguration(nhConfig);
            //Transport
            var transport = endpointConfiguration.UseTransport<SqlServerTransport>()
                            .ConnectionString(ConfigurationManager.ConnectionStrings["NSB_AWS.SqlServerTransport"].ConnectionString);
            transport.DefaultSchema("nsb");

            var routing = transport.Routing();
            routing.RegisterPublisher(typeof(OrderPlaced), "Samples.PubSub.Publisher");

            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.EnableInstallers();

            var endpointInstance = await Endpoint.Start(endpointConfiguration)
                .ConfigureAwait(false);
            await Start(endpointInstance)
                .ConfigureAwait(false);
            await endpointInstance.Stop()
                .ConfigureAwait(false);
        }

        static async Task Start(IEndpointInstance endpointInstance)
        {
            Console.WriteLine("Press '1' to publish the OrderReceived event");
            Console.WriteLine("Press any other key to exit");

            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine();

                var orderReceivedId = Guid.NewGuid();
                if (key.Key == ConsoleKey.D1)
                {
                    var orderReceived = new OrderPlaced
                    {
                        OrderId = orderReceivedId
                    };
                    await endpointInstance.Publish(orderReceived)
                        .ConfigureAwait(false);
                    Console.WriteLine($"Published OrderReceived Event with Id {orderReceivedId}.");
                }
                else
                {
                    return;
                }
            }
        }
    }
 }

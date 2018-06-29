using Infrastructure;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Transport.SQLServer;
using Shared.Events;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace Subscriber1
{
    class Program
    {
        static SendAndProcessEndpoint<BaseEndpointConfig> _endpoint;
        static void Main()
        {
            _endpoint = new SendAndProcessEndpoint<BaseEndpointConfig>(new Subscriber1Config());
            AsyncMain().GetAwaiter().GetResult();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        static async Task AsyncMain()
        {
            _endpoint.Initialize();
            Console.Title = "NSB.Subscriber1";
            await _endpoint.StartEndpoint()
                .ConfigureAwait(false);
            Console.ReadKey();
        }

        static async Task AsyncMain1()
        {
            Console.Title = "Samples.PubSub.Subscriber";
            var endpointConfiguration = new EndpointConfiguration("Samples.PubSub.Subscriber");
            //endpointConfiguration.UsePersistence<LearningPersistence>();
            //var transport = endpointConfiguration.UseTransport<LearningTransport>();

            //Persistence
            var persistence = endpointConfiguration.UsePersistence<NHibernatePersistence>();
            var nhConfig = new NHibernate.Cfg.Configuration();
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider");
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.Sql2008ClientDriver");
            nhConfig.SetProperty(NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MsSql2008Dialect");
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionString, ConfigurationManager.ConnectionStrings["NSB_AWS.NHibernatePersistence"].ConnectionString);
            nhConfig.SetProperty(NHibernate.Cfg.Environment.DefaultSchema, "nsb");
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
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            await endpointInstance.Stop()
                .ConfigureAwait(false);
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            _endpoint.StopEndpoint();
        }
    }
}

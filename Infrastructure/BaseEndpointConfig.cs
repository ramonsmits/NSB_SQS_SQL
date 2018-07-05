using Amazon;
using Amazon.S3;
using Amazon.SQS;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using Shared.Command;
using Shared.Events;
using System;
using System.Net;
using System.Runtime;
using NHibernate.Cfg;
using NServiceBus.Persistence.NHibernate;
using NServiceBus.Transport.SQLServer;
using NServiceBus.Transport;
using Environment = System.Environment;

namespace Infrastructure
{
    public class BaseEndpointConfig : IEndpointConfig
    {
        readonly string _configEndpointName;
        readonly bool _isSendOnly;

        protected BaseEndpointConfig(string endpointName, bool isSendOnly)
        {
            _configEndpointName = endpointName;
            _isSendOnly = isSendOnly;
        }

        public virtual EndpointConfiguration BuildConfig()
        {
            GCSettings.LatencyMode = GCLatencyMode.Batch; // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/latency
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 125; // Querying of DefaultConnectionLimit on dotnet core does not return assigned value
            //logger
            ConfigureNsbLogger();

            //endpoint name
            if (string.IsNullOrEmpty(_configEndpointName))
                throw new ArgumentNullException(_configEndpointName, "Endpoint name cannot be null");

            var endpointConfiguration = new EndpointConfiguration(_configEndpointName);

            endpointConfiguration.LimitMessageProcessingConcurrencyTo(Environment.ProcessorCount * 4);

            if (_isSendOnly)
                endpointConfiguration.SendOnly();

            //Transport
            var transportType = TransportType.Sqs;

            //serializer
            endpointConfiguration.UseSerialization<XmlSerializer>();

            var connectionString = "Server=.;Initial Catalog=support/20180629;Integrated Security=True";
            var persistence = endpointConfiguration.UsePersistence<NHibernatePersistence>();
            var nhConfig = new NHibernate.Cfg.Configuration();
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionProvider, typeof(NHibernate.Connection.DriverConnectionProvider).FullName);
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionDriver, typeof(NHibernate.Driver.Sql2008ClientDriver).FullName);
            nhConfig.SetProperty(NHibernate.Cfg.Environment.Dialect, typeof(NHibernate.Dialect.MsSql2012Dialect).FullName);
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionString, connectionString);
            nhConfig.SetProperty(NHibernate.Cfg.Environment.DefaultSchema, "dbo");

            persistence.UseConfiguration(nhConfig);
            persistence.EnableCachingForSubscriptionStorage(TimeSpan.FromSeconds(10));

            endpointConfiguration.SendFailedMessagesTo("error");
            //endpointConfiguration.AuditProcessedMessagesTo("audit");
            //endpointConfiguration.EnableOutbox();

            switch (transportType)
            {
                case TransportType.Sql:
                    {
                        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
                        transport.ConnectionString(connectionString);
                        transport.DefaultSchema("dbo");
                        transport.Transactions(TransportTransactionMode.ReceiveOnly);
                        BuildEndpointSQLRouting(transport.Routing());
                        break;
                    }
                case TransportType.Sqs:
                    {
                        var region = RegionEndpoint.EUWest1;
                        var S3BucketName = "ramon-sqs";
                        var S3KeyPrefix = "support/20180629";


                        var transport = endpointConfiguration.UseTransport<SqsTransport>();
                        transport.ClientFactory(() => new AmazonSQSClient(
                        new AmazonSQSConfig
                        {
                            RegionEndpoint = region,
                            MaxErrorRetry = 2,
                        }));

                        var s3Configuration = transport.S3(S3BucketName, S3KeyPrefix);
                        s3Configuration.ClientFactory(() => new AmazonS3Client(
                        new AmazonS3Config
                        {
                            RegionEndpoint = region
                        }));

                        //Routing
                        BuildEndpointSQSRouting(transport.Routing());
                        transport.QueueNamePrefix("ramon-support-20180629-");
                        break;
                    }
                case TransportType.Learning:
                    {
                        endpointConfiguration.UsePersistence<LearningPersistence>();
                        var transport = endpointConfiguration.UseTransport<LearningTransport>();
                        endpointConfiguration.DisableFeature<TimeoutManager>(); // REVIEW: Why are you disabling the timeout manager??
                        BuildEndpointLearningRouting(transport.Routing());
                        break;
                    }

                default:
                    throw new Exception("Unexpected Case");
            }


            //Auto installer
            //#if DEBUG
            endpointConfiguration.EnableInstallers();
            //#else
            //           endpointConfiguration.DisableInstallers();
            //#endif


            var performanceCounters = endpointConfiguration.EnableWindowsPerformanceCounters();
            performanceCounters.EnableSLAPerformanceCounters(TimeSpan.FromSeconds(100));


            return endpointConfiguration;
        }

        private void ConfigureNsbLogger()
        {
            var defaultFactory = LogManager.Use<DefaultFactory>();
            defaultFactory.Level(LogLevel.Warn);
        }


        internal virtual RoutingSettings<SqlServerTransport> BuildEndpointSQLRouting(
           RoutingSettings<SqlServerTransport> routing)
        {
            routing.RouteToEndpoint(typeof(PlaceOrder), "NSB.Server");
            routing.RegisterPublisher(typeof(OrderPlaced), "NSB.Server");
            return routing;
        }

        internal virtual RoutingSettings<SqsTransport> BuildEndpointSQSRouting(
          RoutingSettings<SqsTransport> routing)
        {
            routing.RouteToEndpoint(typeof(PlaceOrder), "NSB.Server");
            routing.RegisterPublisher(typeof(OrderPlaced), "NSB.Server");
            return routing;
        }

        internal virtual RoutingSettings<LearningTransport> BuildEndpointLearningRouting(
         RoutingSettings<LearningTransport> routing)
        {
            routing.RouteToEndpoint(typeof(PlaceOrder), "NSB.Server");
            return routing;
        }
    }
}

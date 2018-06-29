using Amazon;
using Amazon.S3;
using Amazon.SQS;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Persistence;
using NServiceBus.Transport.SQLServer;
using Shared.Command;
using Shared.Events;
using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Collections.Generic;

namespace Infrastructure
{
    public class BaseEndpointConfig : IEndpointConfig
    {
        internal string _configEndpointName;
        protected bool _isSendOnly;

        private readonly TimeSpan _slaTime;

        public BaseEndpointConfig() :
            this(null, false)
        {
            _slaTime = TimeSpan.FromSeconds(1);
        }

        public BaseEndpointConfig(string endpointName, bool isSendOnly)
        {
            _configEndpointName = string.IsNullOrEmpty(endpointName) ? GetEndpointName() : endpointName;
            _isSendOnly = isSendOnly;
        }

        public virtual EndpointConfiguration BuildConfig()
        {
            //logger
            ConfigureNsbLogger();

            //endpoint name
            if (string.IsNullOrEmpty(_configEndpointName))
                throw new ArgumentNullException(_configEndpointName, "Endpoint name cannot be null");

            var endpointConfiguration = new EndpointConfiguration(_configEndpointName);
            if (_isSendOnly)
                endpointConfiguration.SendOnly();

            //Transport
            var transportType = ConfigurationManager.AppSettings["transportType"];

            if (string.IsNullOrEmpty(transportType))
                throw new Exception("Provide TransportType in configuration");

            //serializer
            endpointConfiguration.UseSerialization<XmlSerializer>();
           
            var persistence = endpointConfiguration.UsePersistence<NHibernatePersistence>();
            var nhConfig = new NHibernate.Cfg.Configuration();
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider");
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.Sql2008ClientDriver");
            nhConfig.SetProperty(NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MsSql2008Dialect");

            if((TransportType)Convert.ToInt32(transportType)== TransportType.Sql)
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionString, ConfigurationManager.ConnectionStrings["NSB_AWS.SQL.NHibernatePersistence"].ConnectionString);
            else if ((TransportType)Convert.ToInt32(transportType) == TransportType.Sqs)
                nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionString, ConfigurationManager.ConnectionStrings["NSB_AWS.SQS.NHibernatePersistence"].ConnectionString);

            nhConfig.SetProperty(NHibernate.Cfg.Environment.DefaultSchema, "dbo");
            
            persistence.UseConfiguration(nhConfig);
                                          
            

            switch ((TransportType)Convert.ToInt32(transportType))
            {
                case TransportType.Sql:
                    {
                        endpointConfiguration.SendFailedMessagesTo("error");
                        endpointConfiguration.AuditProcessedMessagesTo("audit");


                        var transport = endpointConfiguration.UseTransport<SqlServerTransport>()
                                       .ConnectionString(ConfigurationManager.ConnectionStrings["NSB_AWS.SQL.SqlServerTransport"].ConnectionString);
                        transport.DefaultSchema("dbo");
                        BuildEndpointSQLRouting(transport.Routing());
                        break;
                    }
                case TransportType.Sqs:
                    {
                        //ERROR and AUDIT queue
                        endpointConfiguration.SendFailedMessagesTo("error");
                        endpointConfiguration.AuditProcessedMessagesTo("audit");
                 
                        //S3 Setting
                        var S3BucketName = ConfigurationManager.AppSettings["S3BucketName"];
                        var S3KeyPrefix = ConfigurationManager.AppSettings["S3KeyPrefix"];

                        var transport = endpointConfiguration.UseTransport<SqsTransport>();
                        transport.ClientFactory(() => new AmazonSQSClient(
                        new AmazonSQSConfig
                        {
                            RegionEndpoint = RegionEndpoint.USEast1,
                            MaxErrorRetry=2,
                        }));

                        var s3Configuration = transport.S3(S3BucketName, S3KeyPrefix);
                        s3Configuration.ClientFactory(() => new AmazonS3Client(
                        new AmazonS3Config
                        {
                            RegionEndpoint = RegionEndpoint.USEast1
                        }));

                        //Routing
                        BuildEndpointSQSRouting(transport.Routing());
                        endpointConfiguration.EnableOutbox();
                        break;
                    }
                case TransportType.Learning:
                    {
                        endpointConfiguration.SendFailedMessagesTo("error");
                        endpointConfiguration.AuditProcessedMessagesTo("audit");
                        endpointConfiguration.UsePersistence<LearningPersistence>();
                        var transport = endpointConfiguration.UseTransport<LearningTransport>();
                        endpointConfiguration.DisableFeature<TimeoutManager>();
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

        protected string GetNsbLogPath()
        {
            var cfg = ConfigurationManager.AppSettings["LogPath"] ?? string.Empty;
            return cfg;
        }

        protected LogLevel GetNsbLogLevel()
        {
            var cfg = ConfigurationManager.AppSettings["LogLevel"] ?? string.Empty;

            switch (cfg.ToLower())
            {
                case "debug":
                    return LogLevel.Debug;
                case "info":
                    return LogLevel.Info;
                case "warn":
                    return LogLevel.Warn;
                case "error":
                    return LogLevel.Error;
                case "fatal":
                    return LogLevel.Fatal;
                default:
                    return LogLevel.Info;
            }
        }

        protected string GetAuditQueueName()
        {
            return "audit";
        }

        protected string GetErrorQueueName()
        {
            return "error";
        }


        [SuppressMessage("ReSharper", "NotResolvedInText")]
        protected string GetEndpointName()
        {
            var cfg = ConfigurationManager.AppSettings["EndpointName"] ?? string.Empty;
            if (string.IsNullOrEmpty(cfg))
                throw new ArgumentNullException("EndpointName cannot be null or empty in the endpoint config file");

            return cfg;
        }

        private void ConfigureNsbLogger()
        {
            var defaultFactory = LogManager.Use<DefaultFactory>();
            defaultFactory.Level(GetNsbLogLevel());

            var usePath = true;
            var path = GetNsbLogPath();

            if (!string.IsNullOrEmpty(path))
            {
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                        // Do nothing, default location is used
                        usePath = false;
                    }
                }
            }

            if (usePath)
                defaultFactory.Directory(path);
        }


        internal virtual RoutingSettings<SqlServerTransport> BuildEndpointSQLRouting(
           RoutingSettings<SqlServerTransport> routing)
        {
            routing.RouteToEndpoint(typeof(PlaceOrder), "NSB.Server");
            routing.RegisterPublisher(typeof(OrderPlaced), "NSB.Server");
            routing.RegisterPublisher(typeof(BulkOrderPlaced), "NSB.Server");
            return routing;
        }

        internal virtual RoutingSettings<SqsTransport> BuildEndpointSQSRouting(
          RoutingSettings<SqsTransport> routing)
        {
            routing.RouteToEndpoint(typeof(PlaceOrder), "NSB.Server");
            routing.RegisterPublisher(typeof(OrderPlaced), "NSB.Server");
            routing.RegisterPublisher(typeof(BulkOrderPlaced), "NSB.Server");
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

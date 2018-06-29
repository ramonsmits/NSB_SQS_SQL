using System;
using System.Configuration;
using Autofac.Integration.WebApi;
using Infrastructure;
using NServiceBus;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Shared.Command;
using Amazon.S3;
using Amazon.SQS;
using Autofac;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Transport.SQLServer;
using Amazon;

namespace API
{
    public class WebApiApplication : HttpApplication
    {
        IEndpointInstance _endpointInstance;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            //Initiate NSB
            InitializeEndpoints();

        }

        protected void Application_End()
        {
            _endpointInstance.Stop();
        }

        private void InitializeEndpoints()
        {
            var endpointConfiguration = new EndpointConfiguration("NSB.Server");

            //Transport
            var transportType = ConfigurationManager.AppSettings["transportType"];

            if (string.IsNullOrEmpty(transportType))
                throw new Exception("Provide TransportType in configuration");

            //Persistence
            var persistence = endpointConfiguration.UsePersistence<NHibernatePersistence>();
            var nhConfig = new NHibernate.Cfg.Configuration();
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider");
            nhConfig.SetProperty(NHibernate.Cfg.Environment.ConnectionDriver, "NHibernate.Driver.Sql2008ClientDriver");
            nhConfig.SetProperty(NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MsSql2008Dialect");

            if ((TransportType)Convert.ToInt32(transportType) == TransportType.Sql)
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
                        transport.Routing().RouteToEndpoint(typeof(PlaceOrder),
                           "NSB.Server");
                        break;
                    }
                case TransportType.Sqs:
                    {
                        endpointConfiguration.SendFailedMessagesTo("error");
                        endpointConfiguration.AuditProcessedMessagesTo("audit");

                        var S3BucketName = ConfigurationManager.AppSettings["S3BucketName"];
                        var S3KeyPrefix = ConfigurationManager.AppSettings["S3KeyPrefix"];
                        var transport = endpointConfiguration.UseTransport<SqsTransport>();

                        transport.ClientFactory(() => new AmazonSQSClient(new AmazonSQSConfig
                        {
                            RegionEndpoint = RegionEndpoint.USEast1,
                        }));

                        var s3Configuration = transport.S3(S3BucketName, S3KeyPrefix);

                        s3Configuration.ClientFactory(() => new AmazonS3Client(
                        new AmazonS3Config
                        {
                            RegionEndpoint = RegionEndpoint.USEast1
                        }));


                        endpointConfiguration.EnableOutbox();
                        endpointConfiguration.EnableFeature<TimeoutManager>();

                        transport.Routing().RouteToEndpoint(typeof(PlaceOrder),
                            "NSB.Server");

                        break;
                    }
                case TransportType.Learning:
                    {
                        endpointConfiguration.SendFailedMessagesTo("error");
                        endpointConfiguration.AuditProcessedMessagesTo("audit");
                        endpointConfiguration.UsePersistence<LearningPersistence>();
                      var transport=  endpointConfiguration.UseTransport<LearningTransport>();
                        endpointConfiguration.DisableFeature<TimeoutManager>();
                        transport.Routing().RouteToEndpoint(typeof(PlaceOrder),
                            "NSB.Server");
                        break;
                    }

                default:
                    throw new Exception("Unexpected Case");
            }

            endpointConfiguration.SendOnly();
           

            var _endpointInstance = Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(_endpointInstance);

            // Register MVC controllers.
            builder.RegisterApiControllers(typeof(Controllers.OrderController).Assembly);

            var container = builder.Build();

           // DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver =
                 new AutofacWebApiDependencyResolver(container);
        }

    }
}

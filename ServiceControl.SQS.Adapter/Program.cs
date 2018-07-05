using System;
using System.Threading.Tasks;
using Amazon.S3;
using NServiceBus;
using ServiceControl.TransportAdapter;
using Amazon;
using Amazon.SQS;

namespace ServiceControl.SQS.Adapter
{
    class Program
    {
        static void Main()
        {
            AsyncMain().GetAwaiter().GetResult();
            Console.ReadKey();
        }

        public static async Task AsyncMain()
        {
            Console.Title = "Samples.ServiceControl.SqsTransportAdapter.Adapter";

            var transportAdapterConfig =
                new TransportAdapterConfig<SqsTransport, MsmqTransport>("ServiceControl.SQS.Adapter")
                {
                    EndpointSideAuditQueue = "audit",
                    EndpointSideErrorQueue = "error",
                    EndpointSideControlQueue = "Particular.ServiceControl"
                };

            transportAdapterConfig.CustomizeEndpointTransport(transport =>
            {
                var s3Configuration = transport.S3("ramon-sqs", "support/20180629");

                var region = RegionEndpoint.EUWest1;

                transport.ClientFactory(() => new AmazonSQSClient(new AmazonSQSConfig
                {
                    RegionEndpoint = region,
                }));

                s3Configuration.ClientFactory(() => new AmazonS3Client(
                        new AmazonS3Config
                        {
                            RegionEndpoint = region
                        }));
            });

            var adapter = TransportAdapter.TransportAdapter.Create(transportAdapterConfig);

            await adapter.Start()
                .ConfigureAwait(false);

            Console.WriteLine("Press <enter> to shutdown adapter.");
            Console.ReadLine();

            await adapter.Stop()
                .ConfigureAwait(false);
        }
    }
}

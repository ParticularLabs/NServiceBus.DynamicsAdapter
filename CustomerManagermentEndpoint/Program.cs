﻿namespace CustomerManagermentEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Transport.AzureServiceBus;
    using CustomerManagementMessages;

    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            Console.Title = "Customer Management";

            var endpointConfiguration = new EndpointConfiguration("Samples.ServiceBus.CustomerManagementEndpoint");
            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.AuditProcessedMessagesTo("audit");
            endpointConfiguration.HeartbeatPlugin("particular.servicecontrol");
            endpointConfiguration.UseSerialization<JsonSerializer>();
            endpointConfiguration.EnableInstallers();
            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            var connectionString = Environment.GetEnvironmentVariable("CRM.AzureServiceBus.ConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Could not read the 'CRM.AzureServiceBus.ConnectionString' environment variable. Check the sample prerequisites.");
            }
            transport.ConnectionString(connectionString);
            transport.BrokeredMessageBodyType(SupportedBrokeredMessageBodyTypes.Stream);
            var topology = transport.UseForwardingTopology();
           // topology.NumberOfEntitiesInBundle(1);
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.Recoverability().DisableLegacyRetriesSatellite();

            //Setup routing for our command;
            transport.Routing().RouteToEndpoint(
                messageType: typeof(CreateCustomerTaskRequest),
                destination: "Samples.ServiceBus.CRMAPIGatewayEndpoint");

            transport.Routing().RouteToEndpoint(
                messageType: typeof(UpdateTaskRequest),
                destination: "Samples.ServiceBus.CRMAPIGatewayEndpoint");


            var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);

            try
            {
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
            finally
            {
                await endpointInstance.Stop()
                    .ConfigureAwait(false);
            }

        }
    }

}

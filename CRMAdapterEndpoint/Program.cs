using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport.AzureServiceBus;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using CRMMapping;


namespace CRMAdapterEndpoint
{
    /* This adapter endpoint will be a multihost that will host both a native endpoint that handles messages from Dynamics 365.  The second endpoint
     * will be a publisher of native NServiceBus messages.
     */
    class Program
    {
        internal static IMessageSession MessageSession;

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            Console.Title = "CRMAdapterEndpoint Host";

            var nativeEndpointConfiguration = new EndpointConfiguration("Samples.ServiceBus.CRMAdapterEndpoint");
            //In dynamics the events are configured to go to CRMEvents queue.
            nativeEndpointConfiguration.OverrideLocalAddress("crmevents");
            nativeEndpointConfiguration.SendFailedMessagesTo("error");
            var transport = nativeEndpointConfiguration.UseTransport<AzureServiceBusTransport>();
            var connectionString = Environment.GetEnvironmentVariable("CRM.AzureServiceBus.ConnectionString");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Could not read the 'CRM.AzureServiceBus.ConnectionString' environment variable. Check the sample prerequisites.");
            }
            transport.ConnectionString(connectionString);
            transport.BrokeredMessageBodyType(SupportedBrokeredMessageBodyTypes.Stream);
            var topology = transport.UseForwardingTopology();
            topology.NumberOfEntitiesInBundle(1);

            nativeEndpointConfiguration.UsePersistence<InMemoryPersistence>();
            nativeEndpointConfiguration.UseSerialization<JsonSerializer>();
            nativeEndpointConfiguration.EnableInstallers();

            var recoverability = nativeEndpointConfiguration.Recoverability();
            recoverability.DisableLegacyRetriesSatellite();
            recoverability.AddUnrecoverableException<MapperNotFoundException>();
            

            var nativeEndpointInstance = await Endpoint.Start(nativeEndpointConfiguration)
                .ConfigureAwait(false);

            // can be also injected into a container.  The native endpoint has a Feature that needs the message session.
            MessageSession = nativeEndpointInstance;

            try
            {
                Console.WriteLine("Press any other key to exit");

                while (true)
                {
                    var key = Console.ReadKey();
                    Console.WriteLine();

                    if (key.Key != ConsoleKey.Enter)
                    {
                        return;
                    }

                }
            }
            finally
            {
                await nativeEndpointInstance.Stop()
                    .ConfigureAwait(false);
            }
        }



    }
}

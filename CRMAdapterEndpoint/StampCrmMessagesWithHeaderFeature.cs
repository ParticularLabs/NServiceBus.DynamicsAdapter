using CRMMapping;

namespace CRMAdapterEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;


    public class AdapterInitializer : INeedInitialization
    {
        public void Customize(EndpointConfiguration configuration)
        {
            configuration.Pipeline.Register(typeof(StampCrmMessagesWithHeaderBehavior), "Use a mapper to convert incoming CRM messages into NServiceBus messages (body & header) to allow processing in handlers.");
        }
    }

   
    public class StampCrmMessagesWithHeaderBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {      
                //This pipeline behavior allows us to access the raw message and transform it to an NSB Poco Message
                // Using a Mapper class.
                
                var mappingResult = Mapper.Map(context.Message.Headers, context.Message.Body);
                context.Message.Headers[Headers.EnclosedMessageTypes] = mappingResult.TypeHeaderValue;
                context.UpdateMessage(mappingResult.SerializedMessageBody);
                return next();
        }
    }
}
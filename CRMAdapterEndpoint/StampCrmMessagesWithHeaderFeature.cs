using CRMMapping;

namespace CRMAdapterEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;

    public class StampCrmMessagesWithHeaderFeature : Feature
    {
        internal StampCrmMessagesWithHeaderFeature()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var pipeline = context.Pipeline;
            pipeline.Register<StampCrmMessagesWithHeaderRegistration>();
        }
    }

    public class StampCrmMessagesWithHeaderRegistration : RegisterStep
    {
        public StampCrmMessagesWithHeaderRegistration() : base("StampCrmMessagesWithHeader", typeof(StampCrmMessagesWithHeaderBehavior), "Convert incoming CRM messages into NServiceBus messages (body & header) to allow processing in handlers.")
        {
        }
    }

    public class StampCrmMessagesWithHeaderBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            // TODO: verify if needed (Quick check to see if this is a native message.  We don't want to alter the type otherwise.)
            if (!context.Message.Headers.ContainsKey("NServiceBus.EnclosedMessageTypes"))
            {
                var mappingResult = Mapper.Map(context.Message.Headers, context.Message.Body);
               
                context.Message.Headers[Headers.EnclosedMessageTypes] = mappingResult.TypeHeaderValue;
                context.UpdateMessage(mappingResult.SerializedMessageBody);
            }
            return next();
        }
    }
}
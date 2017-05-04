namespace CRMMapping
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using CRMMapping.Messages;
    using Microsoft.Xrm.Sdk;
    using NServiceBus;
    using Newtonsoft.Json;

    /// <summary>Map CRM entity name and action to the corresponding NServiceBus message</summary>
    public static class Mapper
    {
        public static MappingResult Map(Dictionary<string, string> messageHeaders, byte[] crmRawMessage)
        {
            // Deserialize CRM message into RemoteExecutionContext
            var stream = new MemoryStream(crmRawMessage);
            var remoteExecutionContext = (RemoteExecutionContext)new DataContractJsonSerializer(typeof(RemoteExecutionContext)).ReadObject(stream);

            //Get the Entity and Action from the header in the raw CRM message from Azure. 
            var entityName = messageHeaders["http://schemas.microsoft.com/xrm/2011/Claims/EntityLogicalName"];
            var entityAction = messageHeaders["http://schemas.microsoft.com/xrm/2011/Claims/RequestName"];

            var mapperTypeName = entityName.ToLower() + entityAction.ToLower();

            IMessage targetMessage;

            switch (mapperTypeName)
            {
                case "contactcreate":
                    targetMessage = new ContactCreate(remoteExecutionContext);
                    break;

                case "contactupdate":
                    targetMessage = new ContactUpdate(remoteExecutionContext);
                    break;

                default:
                    //if we don't have a mapper, throw this exception.  It is configured as non-recoverable in the adapter endpoint and won't trigger retry.
                    throw new MapperNotFoundException($"A mapping class is not configured for the entity {entityName} and action {entityAction}.");
            }

            // serialize the message
            var serializedObject = JsonConvert.SerializeObject(targetMessage);
            var bytes = System.Text.Encoding.UTF8.GetBytes(serializedObject);

            return new MappingResult(bytes, targetMessage.GetType().FullName);
        }
    }
}
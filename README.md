 # NServiceBus.DynamicsAdapter #

The DynamicsAdapter repo contains an example application that demonstrates bi-directional integration between cloud hosted Dynamics 365 2016 and an on-premises NServiceBus system using Azure Service Bus Queues. 

The complete hybrid sample application simulates a scenario where the creation of a new contact in CRM triggers an on premises event in NServiceBus.   This event orchestrates the creation a new CRM Task for the new contact that is updated based on the outcome of a simulated fraud screening service endpoint.

While the solution is mostly standard NServiceBus, the projects that facilitate the integration with CRM require some prerequisite setup. 

##Setting up Dynamics 365 2016 for Azure Service Bus Integration##

The sample leverages the built in [Azure ServiceBusPlugin integration](https://msdn.microsoft.com/en-us/library/gg309677.aspx) to send a native CRM event message to Azure that is received and translated by an adapter into a compatible NServiceBus event.  

Dynamics CRM 2016 must be configured to send messages to an Azure Service Bus queue when a new contact is created.   

If you not already a Dynamics CRM customer you can[ enroll in a trial](https://technet.microsoft.com/en-us/library/mt772202.aspx) that can be used for this example.  

For this sample to function you must:
1. Have access to a Dynamics 365 2016 cloud hosted instance.  If you not already a Dynamics CRM customer you can[ enroll in a trial](https://technet.microsoft.com/en-us/library/mt772202.aspx) that can be used for this example.
1. Create an Azure Service Bus Queue [configured correctly ](https://msdn.microsoft.com/en-us/library/mt697580.aspx)to receive messages from CRM.
1. Download the [Dynamics 365 SDK](http://go.microsoft.com/fwlink/?LinkID=627298) and use the plugin registration tool to configure 'Contact Create' to asynchronously send a message to your Azure Service Bus queue.   


    
## The CRM Adapter Endpoint Project - CRMAdapterEndpoint ##

The CRMAdapterEndpoint is responsible for retrieving the raw CRM messages from Azure Service Bus and translating them into properly formated NServiceBus events.  

The project is configured to user the [Azure Service Bus transport](https://docs.particular.net/nservicebus/azure-service-bus/).  The endpoint will receive the raw CRM messages from your Azure Service Bus Queue as provisioned in step 2 above. For the sample to work you must create a system environment variable called CRM.AzureServiceBus.ConnectionString on your machine with your Azure Service Bus connectionstring as it's value.   Note that the other projects in this solution use the same connectionstring.  

```
var nativeEndpointConfiguration = new EndpointConfiguration("Samples.ServiceBus.CRMAdapterEndpoint");
//In dynamics the events are configured to go to CRMEvents queue.
nativeEndpointConfiguration.OverrideLocalAddress("crmevents");
nativeEndpointConfiguration.SendFailedMessagesTo("error");
var transport = nativeEndpointConfiguration.UseTransport<AzureServiceBusTransport>();
var connectionString = Environment.GetEnvironmentVariable("CRM.AzureServiceBus.ConnectionString");  
```    

### The Adapter Implementation###
NServicebus has a variety of extension points in the message pipeline.  In order to get access to the raw message from Dynamics, a custom NServiceBus [Feature](https://docs.particular.net/nservicebus/pipeline/features) has been implemented in [StampCRMMessagesWithHeader.cs](https://github.com/ParticularLabs/NServiceBus.DynamicsAdapter/blob/master/CRMAdapterEndpoint/StampCrmMessagesWithHeaderFeature.cs).  


In the Invoke method, a Mapper is invoked that accepts the raw headers and body and returns a new NServiceBus message type along with the properly serialized body.  The are used to update the message before it proceeds through the pipeline.  



```

public class StampCrmMessagesWithHeaderBehavior : Behavior<IIncomingPhysicalMessageContext>
{
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
           
                var mappingResult = Mapper.Map(context.Message.Headers, context.Message.Body);
               
                context.Message.Headers[Headers.EnclosedMessageTypes] = mappingResult.TypeHeaderValue;
                context.UpdateMessage(mappingResult.SerializedMessageBody);
           
           		return next();
        }
}

```

The [Mapper.cs](https://github.com/ParticularLabs/NServiceBus.DynamicsAdapter/blob/master/CRMMapping/Mapper.cs) class in the CRMMapper project deserializes the raw message into a Dynamics RemoteExecutionContext and uses the entity and action that CRM puts in the headers to initialize the proper type of NServiceBus message that implements IMessage. 

The message type and serialzed body is returned to the mapper and replaces the raw message and type indicating header in the message before it proceeds down the pipeline.   

```
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

            var serializedObject = JsonConvert.SerializeObject(targetMessage);
            var bytes = System.Text.Encoding.UTF8.GetBytes(serializedObject);

            return new MappingResult(bytes, targetMessage.GetType().FullName);
```

The RemoteExecutionContext is mapped to the message within the constructor of each message as shown in the [ContactCreate](https://github.com/ParticularLabs/NServiceBus.DynamicsAdapter/blob/master/CRMMapping/Messages/ContactCreate.cs) message class and constructor.  

```
 public ContactCreate(RemoteExecutionContext context)
        {

            ContactId = context.PrimaryEntityId;
            CreatedById = context.InitiatingUserId;
            CreateDate = context.OperationCreatedOn;

            //We can cast the 'Target' to a late bound CRM Entity type to parse it a bit easier.
            Entity entity = (Entity) context.InputParameters["Target"];

            FullName = entity.GetCrmValue("fullname");
            FirstName = entity.GetCrmValue("firstname");
            LastName = entity.GetCrmValue("lastname");
            Address = entity.GetCrmValue("address1_composite");
            Email = entity.GetCrmValue("emailaddress1");
        }
```



## Dynamics CRM API Gateway ##

The other integration point with Dynamics involves using the Dynamics REST Api to create and modify a task related to the newly created contact. 

The CRMApiGateway project handles command messages and invokes the REST api through a manager class.  

```
 class ContactTaskRequestHandler : IHandleMessages<CreateCustomerTaskRequest>

    {
        private CRMApiManager apiManager;

        public ContactTaskRequestHandler(CRMApiManager apiManager)
        {
            this.apiManager = apiManager;
        }


        public async Task Handle(CreateCustomerTaskRequest message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Creating new task for {message.ContactId}.");
            var newTaskIdUri = await apiManager.CreateTaskForContact(message.ContactId, message.Subject, message.Description, message.Deadline);
            var newTaskGuid = new Guid(newTaskIdUri.Substring(newTaskIdUri.Length - 36 - 1, 36));

            await context.Reply(new CreateCustomerTaskResponse { ContactId = message.ContactId, TaskId = newTaskGuid });
        }
    }

```

Update the connectionstring in the app.config for the URI and user credentials for your CRM instance. 




  
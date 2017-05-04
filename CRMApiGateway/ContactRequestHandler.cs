namespace CRMApiGatewayEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using CustomerManagementMessages;

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
}

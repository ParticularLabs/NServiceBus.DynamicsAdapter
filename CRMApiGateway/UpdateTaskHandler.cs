namespace CRMApiGatewayEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using CustomerManagementMessages;

    class UpdateTaskHandler : IHandleMessages<UpdateTaskRequest>
    {
        private CRMApiManager apiManager;

        public UpdateTaskHandler(CRMApiManager apiManager)
        {
            this.apiManager = apiManager;
        }

        public async Task Handle(UpdateTaskRequest message, IMessageHandlerContext context)
        {

            Console.WriteLine($"Api manager updated task {message.TaskId} assigning to {message.AssignedToUserId}.");
            var contactId = await apiManager.UpdateTask(message.TaskId,message.MarkComplete,message.AssignedToUserId,message.Description).ConfigureAwait(false);

          
            await context.Reply(new UpdateTaskResponse { TaskId = message.TaskId,  Success = true }).ConfigureAwait(false);
        }
    }
}

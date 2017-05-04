using CRMApiGatewayEndpoint;

namespace CRMApiGateway
{
    using System;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Tooling.Connector;

    class CRMSdkManager
    {
        private CrmServiceClient serviceclient { get; }

        public CRMSdkManager()
        {
            //Hook up the connection.
            var connectionString = "AuthType=Office365;" +  Environment.GetEnvironmentVariable("Dynamics365API.ConnectionString");
            serviceclient  = new CrmServiceClient(connectionString);
        }

        public  string  CreateTaskForContact(string contactId, string subject, string description, DateTimeOffset deadline)
        {
            // Create associated task (late bound)
            var task = new Entity("task") { Id = Guid.NewGuid() };
            task["regardingobjectid"] = new EntityReference("contact", new Guid(contactId));
            task["subject"] =subject;
            task["description"] = description;
            task["scheduledend"] = deadline;
            task["statecode"] = (int)CRMApiManager.TaskState.Open;
            task["statuscode"] = (int)CRMApiManager.TaskStatus.InProgress;
            serviceclient.Create(task);

            return task.Id.ToString();
        }
    }
}

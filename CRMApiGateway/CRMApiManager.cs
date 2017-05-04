namespace CRMApiGatewayEndpoint
{
    using Microsoft.Crm.Sdk.Samples.HelperCode;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Text;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net;


    public class CRMApiManager : IDisposable
    {
        private HttpClient httpClient { get; }

        public CRMApiManager()
        {
            Configuration config = new FileConfiguration(null);

            Authentication auth = new Authentication(config);
            //Next use a HttpClient object to connect to specified CRM Web service.
            httpClient = new HttpClient(auth.ClientHandler, true);
            //Define the Web API base address, the max period of execute time, the
            // default OData version, and the default response payload format.
            httpClient.BaseAddress = new Uri(config.ServiceUrl + "api/data/v8.1/");
            httpClient.Timeout = new TimeSpan(0, 2, 0);
            httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }


        public async Task<string> CreateTaskForContact(Guid contactId, string subject, string description, DateTimeOffset deadline)
        {
            //Get the URI from the connectionstring and build the proper Customer URI
            //var connection = System.Configuration.ConfigurationManager.ConnectionStrings["default"].ConnectionString;
            // var connection = System.Environment.GetEnvironmentVariable("Dynamics365API.ConnectionString");
            //var crmcontactURI = connection.Substring(connection.IndexOf('=') + 1, (connection.IndexOf(';')) - (connection.IndexOf('=') + 1)) + $"api/data/v8.1/contacts({contactId})" ;

            var crmcontactUri = httpClient.BaseAddress.OriginalString + $"contacts({contactId})";
            var task = new JObject
            {
                {"subject", subject},
                {"description", description},
                {"scheduledend", deadline},
                {"statecode", 0},
                {"statuscode", 3},
                {"regardingobjectid_contact@odata.bind", crmcontactUri}
            };

            HttpRequestMessage createRequest = new HttpRequestMessage(HttpMethod.Post, "tasks");
            createRequest.Content = new StringContent(task.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage createResponse = await httpClient.SendAsync(createRequest);
            if (createResponse.StatusCode == HttpStatusCode.NoContent)
            {
                var newtaskuri = createResponse.Headers.GetValues("OData-EntityId").
                    FirstOrDefault();

                Console.WriteLine("Task '{0}' created. {1}", task.GetValue("subject"), newtaskuri);
                return newtaskuri;
            }

            Console.WriteLine("Failed to create task for reason: {0}.", createResponse.ReasonPhrase);
            throw new CrmHttpResponseException(createResponse.Content);
        }

        internal enum TaskStatus
        {
            InProgress = 3,
            Completed = 5
        }

        internal enum TaskState
        {
            Open = 0,
            Completed = 1,
            Cancelled = 2
        }

        public async Task<Guid> UpdateTask(Guid taskId, bool markAsCompleted, Guid assignTo, string description)
        {
            //Get the URI from the connectionstring and build the proper Customer URI
            Console.WriteLine($"Updating Task {taskId} with Mark complete = {markAsCompleted}");
            var taskUri = httpClient.BaseAddress.OriginalString + $"tasks({taskId})";


            var taskAdd = new JObject();

            if (markAsCompleted)
            {
                taskAdd.Add("statuscode", (int)TaskStatus.Completed);
                taskAdd.Add("statecode", (int)TaskState.Completed);
                taskAdd.Add("percentcomplete", 100);
            }
            else
            {
                taskAdd.Add("statuscode", (int)TaskStatus.InProgress);
                taskAdd.Add("statecode", (int)TaskState.Open);
            }
            taskAdd.Add("description", description);
            //  taskAdd.Add("owninguser", assignTo);

            HttpRequestMessage updateRequest1 = new HttpRequestMessage(new HttpMethod("PATCH"), taskUri);
            updateRequest1.Content = new StringContent(taskAdd.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage updateResponse1 = await httpClient.SendAsync(updateRequest1);
            if (updateResponse1.StatusCode == HttpStatusCode.NoContent) //204
            {

                Console.WriteLine($"Task {taskId} has been updated");
                return new Guid();
            }

            //Console.WriteLine("Failed to update contact for reason: {0}",
            //  updateResponse1.ReasonPhrase);
            throw new CrmHttpResponseException(updateResponse1.Content);
        }

        public void Dispose()
        {
            if (this.httpClient != null)
            {
                httpClient.CancelPendingRequests();
                httpClient.Dispose();
            }
        }
    }
}
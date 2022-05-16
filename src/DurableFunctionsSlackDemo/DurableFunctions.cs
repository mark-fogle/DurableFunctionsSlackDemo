using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DurableFunctionsSlackDemo;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]
namespace DurableFunctionsSlackDemo
{
    public class DurableFunctions
    {
        [FunctionName(nameof(OrchestratorFunction))]
        public async Task<List<string>> OrchestratorFunction(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var outputs = new List<string>();

            await context.CallActivityAsync(nameof(SlackApprovalFunctions.SendSlackMessage),
                $"Starting processing for ID: {context.InstanceId}");

            await context.CallActivityAsync(nameof(SlackApprovalFunctions.SendSlackApprovalActivity), context.InstanceId);

            var approvalValue = await context.WaitForExternalEvent<string>(EventNames.ApprovalEvent);

            await context.CallActivityAsync(nameof(SlackApprovalFunctions.SendSlackMessage),
                $"ID: {context.InstanceId} {approvalValue}");

            log.LogInformation("Received Approval Value: {approvalValue}", approvalValue);

            await context.CallActivityAsync(nameof(SlackApprovalFunctions.SendSlackMessage),
                $"ID: {context.InstanceId} Processing Complete.");

            context.SetCustomStatus(approvalValue);

            return outputs;
        }

        [FunctionName(nameof(TriggerOrchestration))]
        public static async Task<HttpResponseMessage> TriggerOrchestration(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var instanceId = await starter.StartNewAsync(nameof(OrchestratorFunction), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
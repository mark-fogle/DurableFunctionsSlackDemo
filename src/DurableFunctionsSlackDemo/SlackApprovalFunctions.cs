using System;
using System.Threading.Tasks;
using DurableFunctionsSlackDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DurableFunctionsSlackDemo;

public class SlackApprovalFunctions
{
    private readonly ISlackApprovalService _slackApprovalService;

    public SlackApprovalFunctions(ISlackApprovalService slackApprovalService)
    {
        _slackApprovalService = slackApprovalService ?? throw new ArgumentNullException(nameof(slackApprovalService));
    }

    /// <summary>
    /// This function sends a message to Slack
    /// </summary>
    /// <param name="messageText"></param>
    /// <returns></returns>
    [FunctionName(nameof(SendSlackMessage))]
    public async Task SendSlackMessage([ActivityTrigger] string messageText)
    {
        await _slackApprovalService.SendMessageAsync(messageText);
    }

    /// <summary>
    /// This function sends a templated message to Slack to request approval
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="log"></param>
    /// <returns></returns>
    [FunctionName(nameof(SendSlackApprovalActivity))]
    public async Task SendSlackApprovalActivity([ActivityTrigger] string instanceId, ILogger log)
    {
        log.LogInformation($"Sending approval webhook for ID:{instanceId}.");
        await _slackApprovalService.RequestApproval(instanceId);
    }

    /// <summary>
    /// This function handles the approval response from Slack and sends an event to the orchestrator
    /// </summary>
    /// <param name="req"></param>
    /// <param name="client"></param>
    /// <param name="log"></param>
    /// <returns></returns>
    [FunctionName(nameof(HandleSlackApprovalResponse))]
    public async Task<IActionResult> HandleSlackApprovalResponse(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest  req,
        [DurableClient] IDurableOrchestrationClient client,
        ILogger log)
    {
        //Extract payload from Slack which is form url encoded
        var payload = req.Form["payload"];

        if (string.IsNullOrEmpty(payload))
            return new BadRequestResult();

        log.LogInformation(payload);

        var request = JsonConvert.DeserializeObject<dynamic>(payload);

        //Extract response values we care about
        var responseUrl = request.response_url.ToString();
        string instanceId = request.actions[0].block_id;
        string approvalValue = request.actions[0].value;
        string userName = request.user.name;

        //Respond to Slack to update message
        await _slackApprovalService.SendApprovalResponseAsync(responseUrl, instanceId, approvalValue, userName);

        //Raise event to continue orchestration flow
        await client.RaiseEventAsync(instanceId, EventNames.ApprovalEvent, approvalValue);

        return new OkResult();
    }
}
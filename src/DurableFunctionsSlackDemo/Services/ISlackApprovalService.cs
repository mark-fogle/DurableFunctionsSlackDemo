using System.Net.Http;
using System.Threading.Tasks;

namespace DurableFunctionsSlackDemo.Services;

public interface ISlackApprovalService
{
    Task<HttpResponseMessage> RequestApproval(string instanceId);

    Task<HttpResponseMessage> SendApprovalResponseAsync(string responseUrl,
        string instanceId,
        string approvalValue,
        string userName);

    Task<HttpResponseMessage> SendMessageAsync(string messageText);
}
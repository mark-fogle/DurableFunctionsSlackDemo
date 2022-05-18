using System.Net;
using System.Text;
using System.Net.Http.Headers;
using AutoFixture.Xunit2;
using DurableFunctionsSlackDemo;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DurableFunctionsSlackDemoTests
{
    public class DurableFunctionsSlackDemoTests
    {
        private readonly Mock<IDurableClient> _durableClientMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly Mock<IDurableOrchestrationContext> _mockContext;
        private readonly DurableFunctions _sut;

        public DurableFunctionsSlackDemoTests()
        {
            var instanceId = Guid.NewGuid().ToString();
            // Mock IDurableClient
            _durableClientMock = new Mock<IDurableClient>();

            // Mock ILogger
            _loggerMock = new Mock<ILogger>();

            // Mock CreateCheckStatusResponse method
            _durableClientMock
                // Notice that even though the HttpStart function does not call IDurableClient.CreateCheckStatusResponse() 
                // with the optional parameter returnInternalServerErrorOnFailure, moq requires the method to be set up
                // with each of the optional parameters provided. Simply use It.IsAny<> for each optional parameter
                .Setup(x => x.CreateCheckStatusResponse(It.IsAny<HttpRequestMessage>(), instanceId, It.IsAny<bool>()))
                    .Returns(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(string.Empty),
                        Headers =
                        {
                            RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10))
                        }
                    });

            _mockContext = new Mock<IDurableOrchestrationContext>();

            _sut = new DurableFunctions();
        }

        [Theory, AutoData]
        public async Task TriggerOrchestration_ShouldStartNewOrchestration(Guid instanceId)
        {
            _durableClientMock
                .Setup(x => x.StartNewAsync(nameof(DurableFunctions.OrchestratorFunction), null))
                .ReturnsAsync(instanceId.ToString);

            // Call Orchestration trigger function
            await DurableFunctions.TriggerOrchestration(
                new HttpRequestMessage
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                    RequestUri = new Uri("http://localhost:7071/orchestrators/OrchestratorFunction"),
                },
                _durableClientMock.Object,
                _loggerMock.Object);

            _durableClientMock
                .Verify(x => x.StartNewAsync(nameof(DurableFunctions.OrchestratorFunction), null));

            _durableClientMock.Verify(x=>x.CreateCheckStatusResponse(It.IsAny<HttpRequestMessage>(), instanceId.ToString(), false));
        }

        [Theory, AutoData]
        public async Task OrchestratorFunction_SendsSlackApproval(Guid instanceId)
        {
            _mockContext.Setup(x => x.InstanceId).Returns(instanceId.ToString());

            await _sut.OrchestratorFunction(_mockContext.Object, _loggerMock.Object);

            _mockContext.Verify(x=>x.CallActivityAsync(nameof(SlackApprovalFunctions.SendSlackApprovalActivity), instanceId.ToString()));
        }

        [Theory, AutoData]
        public async Task OrchestratorFunction_SetsCustomStatusToSlackApprovalEventValue(Guid instanceId, string approvalStatus)
        {
            _mockContext.Setup(x => x.InstanceId).Returns(instanceId.ToString());

            _mockContext.Setup(x=>x.WaitForExternalEvent<string>(EventNames.ApprovalEvent)).ReturnsAsync(approvalStatus);

            await _sut.OrchestratorFunction(_mockContext.Object, _loggerMock.Object);

            _mockContext.Verify(x=>x.SetCustomStatus(approvalStatus));
        }
    }
}
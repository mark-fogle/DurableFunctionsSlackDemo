using DurableFunctionsSlackDemo.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableFunctionsSlackDemo;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;
        builder.Services.AddSingleton(configuration.GetSection(nameof(SlackApprovalServiceOptions))
            .Get<SlackApprovalServiceOptions>());
        builder.Services.AddHttpClient<ISlackApprovalService, SlackApprovalService>();
    }
}
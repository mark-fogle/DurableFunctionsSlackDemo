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
        var slackApprovalServiceOptions = new SlackApprovalServiceOptions();
        configuration.GetSection(nameof(SlackApprovalServiceOptions))
            .Bind(slackApprovalServiceOptions);
        builder.Services.AddSingleton(slackApprovalServiceOptions);
        builder.Services.AddHttpClient<ISlackApprovalService>();
        builder.Services.AddScoped<ISlackApprovalService, SlackApprovalService>();
    }
}
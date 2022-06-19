using DurableFunctionsSlackDemo;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DurableFunctionsSlackDemoTests
{
    public class StartupTests
    {
        private readonly IHost _host;

        public StartupTests()
        {
            _host = new HostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(
                        new [] { 
                            new KeyValuePair<string, string>("SlackApprovalServiceOptions:SlackWebhookUrl", "None") 
                        }
                    );
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<SlackApprovalFunctions>();
                })
                .ConfigureWebJobs((context, builder) =>
                {
                    new Startup().Configure(new WebJobsBuilderContext
                    {
                        Configuration = context.Configuration
                    }, builder);
                })
                .Build();
        }

        [Fact]
        public void ServicesShouldBeAbleToResolveSlackApprovalFunctions()
        {
            _host.Services.GetRequiredService<SlackApprovalFunctions>();
        }

        
    }
}
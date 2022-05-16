using System;
using Azure.Identity;
using DurableFunctionsSlackDemo.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableFunctionsSlackDemo;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        //Get KeyVaultUrl from configuration if provided
        var configuration = builder.ConfigurationBuilder.Build();
        var keyVaultUrl = configuration["KeyVaultUrl"];

        // If KeyVaultUrl is provided then register Key Vault configuration provider
        if (!string.IsNullOrEmpty(keyVaultUrl))
        {
            builder.ConfigurationBuilder.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                new DefaultAzureCredential());
        }

        base.ConfigureAppConfiguration(builder);
    }

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
function ZipDeploy-AzureFunction([string]$projectPath, [string]$resourceGroupName, [string]$functionAppName, [string]$publishFolder){
    
    az webapp config appsettings set -g $resourceGroupName -n $functionAppName --settings WEBSITE_RUN_FROM_PACKAGE=1
    az webapp config appsettings set -g $resourceGroupName -n $functionAppName --settings FUNCTIONS_EXTENSION_VERSION=~4
    az webapp config appsettings set -g $resourceGroupName -n $functionAppName --settings FUNCTIONS_WORKER_RUNTIME=dotnet

    dotnet publish -c Release $projectPath
    $publishZip = "$($functionAppName).zip"
    $publishFolder = $projectPath + $publishFolder

    if(Test-path $publishZip) {Remove-item $publishZip}
    Compress-Archive -Path $publishFolder -DestinationPath $publishZip -CompressionLevel Optimal

    az functionapp deployment source config-zip `
        -g $resourceGroupName -n $functionAppName --src $publishZip

    Remove-Item $publishZip
}

$prefix = $args[0]

dotnet restore $PSScriptRoot/../src/DurableFunctionsSlackDemo/DurableFunctionsSlackDemo.csproj

#Deploy Raw To Epoch
ZipDeploy-AzureFunction -projectPath "$($PSScriptRoot)/../src/DurableFunctionsSlackDemo" `
    -resourceGroupName "$($prefix)_rg" -functionAppName "$($prefix)-durable-functions-slack-demo" -publishFolder "/bin/Release/net6.0/publish/*"




# Prerequisite: Install Azure cli: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
# After install success => login azure by open cmd: az login


$Source = $args[0]

$zipPublishPath = '.\\' + $Source + '\\*'
$sourcePublishPath = '.\\' + $Source + '\\publish.zip'

$appServiceName = "sieupham301"
$resourceGroupName = "learn-605cd160-9968-4243-9f79-a8cb9de1b331"


powershell Compress-Archive -Path $zipPublishPath -DestinationPath $sourcePublishPath -Force

az webapp deployment source config-zip --src $sourcePublishPath -n $appServiceName -g $resourceGroupName
# Prerequisite: Install Azure cli: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
# After install success => login azure by open cmd: az login

$ENV = 'Production'
$BUILD_CONFIG = 'Release'
$DOTNET_VERSION = 'net7.0'
$SLN = '.\\SourceCode\\src\\DemoCICD.API\\DemoCICD.API.csproj'

$publishPath = '.\\SourceCode\\src\\DemoCICD.API\\bin\\Release\\net7.0\\publish\\'
$zipPublishPath = '.\\SourceCode\\src\\DemoCICD.API\\bin\\Release\\net7.0\\publish\\*'
$sourcePublishPath = '.\\SourceCode\\src\\DemoCICD.API\\bin\\Release\\net7.0\\publish\\publish.zip'

$appServiceName = "sieupham301"
$resourceGroupName = "learn-b1647810-80f4-4f2b-9575-40eef632eba9"

$GitToken = 'dgk7r4t3wm7uznn27u6uqflwlhucczn4tbcfdbzszxrv7ijgizqq'
$GitUrl = 'https://'+$GitToken+'@dev.azure.com/TDSolutionArchitecture/DemoCICD/_git/DemoCICD'
$GitBranch = 'PROD'

$sourceCode = 'C:\WWW\AZURE\SourceCode'


Remove-Item $sourceCode -Recurse -Force

git clone $GitUrl --branch $GitBranch --single-branch $sourceCode


# restore
dotnet restore $SLN

# clean
dotnet clean $SLN

dotnet build $SLN --configuration $BUILD_CONFIG

dotnet publish $SLN /p:Configuration=$BUILD_CONFIG /p:EnvironmentName=$ENV -o $publishPath

powershell Compress-Archive -Path $zipPublishPath -DestinationPath $sourcePublishPath -Force

az webapp deployment source config-zip --src $sourcePublishPath -n $appServiceName -g $resourceGroupName
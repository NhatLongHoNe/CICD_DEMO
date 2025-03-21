# Prerequisite: Install Azure cli: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
# After install success => login azure by open cmd: az login

$ENV = 'Production'
$BUILD_CONFIG = 'Release'
$DOTNET_VERSION = 'net7.0'
$BASE_PATH = '.\\SourceCode\\src\\DemoCICD.API\\'
$SLN = $BASE_PATH + 'DemoCICD.API.csproj'

$publishPath = $BASE_PATH + 'bin\\'+$BUILD_CONFIG+'\\'+$DOTNET_VERSION+'\\publish\\'
$zipPublishPath = $BASE_PATH + 'bin\\'+$BUILD_CONFIG+'\\'+$DOTNET_VERSION+'\\publish\\*'
$sourcePublishPath = $BASE_PATH + 'bin\\'+$BUILD_CONFIG+'\\'+$DOTNET_VERSION+'\\publish\\publish.zip'

$appServiceName = "sieupham301"
$resourceGroupName = "learn-6d4ac140-efcb-4bc4-a632-b88a27a28e03"

$GitToken = 'vghzcc3woo6cqdbdxgyqpkzrt7pwq4zfe4t3cwj2d6xalytivgdq'
$GitUrl = 'https://'+$GitToken+'@dev.azure.com/TDSolutionArchitecture/DemoCICD/_git/DemoCICD'
$GitBranch = 'PROD'

$sourceCode = '.\\SourceCode\\'
$backupFolder = '.\\PROD_BACKUP\\'
$BACKUP = $backupFolder + 'PROD_' + (Get-Date).ToString('yyyyMMdd_hhmmsstt')
$FileRollBack = $backupFolder +'azure_rollback.ps1'

function main {
	backup_clonecode
	create_file_rollback
	publish_process
	publish_to_azure
}

function backup_clonecode{
	# Root folder backup
	if (Test-Path $backupFolder) {
		Write-Host "Folder Backup Exists"
	}
	else {
		#PowerShell Create directory if not exists
		New-Item $backupFolder -ItemType Directory
		Write-Host "Folder Backup Created successfully"
	}
	
	# Check soucecode, we can backup
	if (Test-Path $sourceCode) {
		Write-Host "Folder SourceCode Exists"
		# backup
		
		if (Test-Path $zipPublishPath){
			
			Copy-Item -Path $zipPublishPath -Destination (New-Item $BACKUP -ItemType Directory) -Recurse
		}
		else{
			Write-Host "Don't have resource for backup!!!"
		}
		
		# Delete folder to clone code
		Remove-Item $sourceCode -Recurse -Force
		#New-Item $sourceCode -ItemType Directory
		git clone $GitUrl --branch $GitBranch --single-branch $sourceCode
		
	}
	else {
		# Create new folder sourceCode and clone
		#New-Item $sourceCode -ItemType Directory
		git clone $GitUrl --branch $GitBranch --single-branch $sourceCode
	}
}

function publish_process{
	# restore
	dotnet restore $SLN

	# clean
	dotnet clean $SLN
	
	# build
	dotnet build $SLN --configuration $BUILD_CONFIG
	
	# publish
	dotnet publish $SLN /p:Configuration=$BUILD_CONFIG /p:EnvironmentName=$ENV -o $publishPath
	
	# zip publish
	powershell Compress-Archive -Path $zipPublishPath -DestinationPath $sourcePublishPath -Force
}

function publish_to_azure{
	az webapp deployment source config-zip --src $sourcePublishPath -n $appServiceName -g $resourceGroupName
}

function create_file_rollback{
	if (Test-Path $FileRollBack) {
		Write-Host "File azure_rollback has been Existed."
		Remove-Item $FileRollBack
	}
	
	New-Item $FileRollBack -ItemType File
	Add-Content $FileRollBack "`# Prerequisite: Install Azure cli: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"
	Add-Content $FileRollBack "`# After install success `=`> login azure by open cmd`: az login"
	
	Add-Content $FileRollBack "`$Source `= `$args`[0`]"
	Add-Content $FileRollBack "`$zipPublishPath = `'.`\`\`' `+ `$Source `+ `'`\`\`*`'"
	Add-Content $FileRollBack "`$sourcePublishPath = `'.`\`\`' `+ `$Source `+ `'`\`\publish`.zip`'"
	Add-Content $FileRollBack "`$appServiceName = $appServiceName"
	Add-Content $FileRollBack "`$resourceGroupName = $resourceGroupName"
	Add-Content $FileRollBack "powershell Compress-Archive -Path `$zipPublishPath -DestinationPath `$sourcePublishPath -Force"
	Add-Content $FileRollBack "az webapp deployment source config-zip --src `$sourcePublishPath -n `$appServiceName -g `$resourceGroupName"
	
	
}

main


# Clone
#

#
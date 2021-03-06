<#
	.SYNOPSIS
		Provides a central storage location for constant and global variables

	.Created
		3/23/2015 - Mike Veazie 
#>

if(!$__IsLoadedConstants__)
{
	#Region Util
	
		# Flag to keep track if we've loaded these or not
		New-Variable -Name "__IsLoadedConstants__" -Option:ReadOnly -Scope "Script" -Value $true -Force
		
		New-Variable -Name "AzureProvisionSettingsFile" -Option:ReadOnly -Scope "Global" -Value '.\conf\azureprovisionsettings.json' -Force
		
		New-Variable -Name "AzureProvisionLogFile" -Option:ReadOnly -Scope "Global" -Value (Join-Path -Path $Env:TEMP -ChildPath 'azureserviceprovision.log') -Force
		
		New-Variable -Name "AzureProvisionSettings" -Scope "Global" -Value $null -Force
		
		New-Variable -Name "AzurePSModules" -Option:ReadOnly -Scope "Global" -Force `
			-Value @{
				"AzurePrimary" = "Azure"
			}
		
		New-Variable -Name AzurePSModulesLoaded -Value $false -Scope "Global" -Force
				
		New-Variable -Name "DependentGACdDLLs" -Option:ReadOnly -Scope "Global" -Force -Value @(
			"System.Windows.Forms",
			"System.Drawing"
		)
		
		# Used to get the install location of the Azure SDK
		# Right now, we support 2.5, 2.6, and 2.7
		if(Test-Path 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows Azure Libraries for .NET\v2.5')
		{
			New-Variable -Name "AzureSDKRegKey" -Option:ReadOnly -Scope "Global" -Force `
				-Value 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows Azure Libraries for .NET\v2.5'
		}
		elseif (Test-Path 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows Azure Libraries for .NET\v2.6')
		{
			New-Variable -Name "AzureSDKRegKey" -Option:ReadOnly -Scope "Global" -Force `
				-Value 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows Azure Libraries for .NET\v2.6'
		}
		elseif (Test-Path 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows Azure Libraries for .NET\v2.7')
		{
			New-Variable -Name "AzureSDKRegKey" -Option:ReadOnly -Scope "Global" -Force `
				-Value 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows Azure Libraries for .NET\v2.7'
		}
	
	#EndRegion Util
	
	#Region Storage
	
		#Relative to the Azure Install Location
		New-Variable -Name "AzureStorageAssembly" -Option:ReadOnly -Scope "Global" -Force `
			-Value 'ToolsRef\Microsoft.WindowsAzure.Storage.dll'
		 
		#The Replication value  in sthe azure provision settings will be looked up here
		#	All whitespace, hyphens, and underscores are removed, and the value is compared in all lowercase
		New-Variable -Name "ReplicationTypes" -Option:ReadOnly -Scope "Global" -Force `
			-Value @{
				"" = "NO_REPLICATION_DEFINED"
				"georedundant" = "Standard_GRS"
				"locallyredundant" = "Standard_LRS"
				"zoneredundant" = "Standard_ZRS"
				"readaccessgeoredundant" = "Standard_RAGRS"
				"standardgrs" = "Standard_GRS"
				"standardlrs" = "Standard_LRS"
				"standardzrs" = "Standard_ZRS"
				"standardragrs" = "Standard_RAGRS"
				"grs" = "Standard_GRS"
				"lrs" = "Standard_LRS"
				"zrs" = "Standard_ZRS"
				"ragrs" = "Standard_RAGRS"
				"premiumgrs" = "Premium_GRS"
				"premiumlrs" = "Premium_LRS"
				"premiumzrs" = "Premium_ZRS"
				"premiumragrs" = "Premium_RAGRS"
				"pgrs" = "Premium_GRS"
				"plrs" = "Premium_LRS"
				"pzrs" = "Premium_ZRS"
				"pragrs" = "Premium_RAGRS"
			}
	
		#The Replication value  in sthe azure provision settings will be looked up here
		#	All whitespace, hyphens, and underscores are removed, and the value is compared in all lowercase
		New-Variable -Name "ContainerAccessTypes" -Option:ReadOnly -Scope "Global" -Force `
			-Value @{
				"" = "NO_ACCESS_DEFINED"
				"publicblob" = "Blob"
				"publiccontainer" = "Container"
				"private" = "Off"		
			}
			
		New-Variable -Name 'MaxStorageAccountNameLength' -Option:ReadOnly -Scope "Global" -Force -Value 24
		New-Variable -Name 'MaxStorageContainerNameLength' -Option:ReadOnly -Scope "Global" -Force -Value 63
		
		#The account name and key will be dynamically updated as we iterate over storage accounts
		New-Variable -Name 'StorageConnectionString' -Scope "Global" -Force -Value "DefaultEndpointsProtocol=https;AccountName=|ACCOUNT_NAME|;AccountKey=|ACCOUNT_KEY|"
			
	#EndRegion Storage
	
	#Region ServiceBus
	
		#Relative to the Azure Install Location
		New-Variable -Name "ServiceBusAssembly" -Option:ReadOnly -Scope "Global" -Force `
		 -Value 'ToolsRef\Microsoft.ServiceBus.dll'
		 
		#Used to build out the event hub connection string. EntityPath is the name of the event hub reletive to the service bus namespace
		New-Variable -Name 'EventHubConnectionString' -Scope "Global" -Force -Value `
			"Endpoint=|NAMESPACE_ABSOLUTE_URI|;SharedAccessKeyName=|SHARED_ACCESS_KEY_NAME|;SharedAccessKey=|SHARED_ACCESS_KEY|;EntityPath=|ENTITY_PATH|"
		 
	#EndRegion ServiceBus
	
	#Region StreamAnalytics
	
	#EndRegion StreamAnalytics
	
	#Region Enums

		$enumDef = @'
			namespace MTC.Enums {
				public enum TraceSeverity { 
					Verbose, 
					Medium,
					Warning,
					Error,
					Exception
				}
			}
'@
		Add-Type -Language CSharp -TypeDefinition $enumDef

	#EndRegion Enums
}

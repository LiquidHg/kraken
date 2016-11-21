param(
	[boolean]$FullInstall = $true
	#TODO your SharePoint server name here
	, [string]$TargetUrl = "http://yoursharepointservername.local"
	, [string]$TargetWebApp = "http://yoursharepointservername.local"
	, [string]$SlnName = ""
)

#
# See the following KB article if you see the error below.
# "The local farm is not accessible. Cmdlets with FeatureDependencyId are not registered."
# Followed by
# "Get-SPSolution : Microsoft SharePoint is not supported with version 4.0.x of the Microsoft .Net Runtime."
# http://support.microsoft.com/kb/2796733
# and http://blogs.technet.com/b/alexsearch/archive/2013/12/11/sharepoint-2010-powershell-incompatibility-with-net-4-x.aspx
#

$SPVersion = "2013"
$SPHiveNum = "15"

# This script is slightly outdated due to changes in the name
# of the WSP files for 2010 and 2013 versions.
# So the above changes haven't been tested as of 2016-04-20

$SlnName_Farm = "Kraken.SPFarmLibrary.$SPVersion.wsp"
$SlnName_Logging = "Kraken.SPLoggingCategories.$SPVersion.wsp"

$SharePointRoot = "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\$SPHiveNum"

Write-Host "Including Fucntion Libraries" -ForegroundColor Green
. ./SPDeployFunctions.ps1
Test-SPDeployFunctions
Add-SPSnapIn
Start-AdminService

## Not needed the WSP are already in the deploy folder now
# Write-Host "Copying WSP Packages from Source." -ForegroundColor Green
# Copy-Item -Path ..\$SlnName -Destination .
# Copy-Item -Path ..\$SlnName_Farm -Destination .
# Copy-Item -Path ..\$SlnName_Logging -Destination .

$wspPath = "$(Get-ScriptDirectory)\$SlnName"
$wspPath_Farm14 = "$(Get-ScriptDirectory)\$SlnName_Farm"
$wspPath_Logging = "$(Get-ScriptDirectory)\$SlnName_Logging"

Write-Host "Checking Solution Dependencies" -ForegroundColor Green
RemoveAdd-Solution -SlnName $SlnName_Farm -WspPath $wspPath_Farm14
RemoveAdd-Solution -SlnName $SlnName_Logging -WspPath $wspPath_Logging

## Handles uninstall (retract), remove (delete), add (upload), and install (deploy)
## Write-Host "Removing/Adding Solution to Farm" -ForegroundColor Green
## RemoveAdd-Solution -SlnName $SlnName  -WspPath $wspPath ## -TargetUrl $TargetUrl

if ($FullInstall) {
	Write-Host "Restarting Timer Job Service" -ForegroundColor Green
	Restart-Service "SPTimerV4" ## -displayname "SharePoint 2010 Timer"
	Write-Host "Restarting IIS" -ForegroundColor Green
	iisreset
}

Write-Host "Activating Logging Categories..." -ForegroundColor Green
Activate-SPFeature -FeatureId f0511646-a032-4cfb-8d81-2ecad3548ce3

Write-Host "Done." -ForegroundColor Green
Read-Host "Press ENTER."

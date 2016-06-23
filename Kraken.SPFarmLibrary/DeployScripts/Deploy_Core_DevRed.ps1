param(
	[boolean]$FullInstall = $true
	, [string]$TargetUrl = "http://portal.red.lab.colossusconsulting.com"
	, [string]$TargetWebApp = "http://portal.red.lab.colossusconsulting.com"
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

$SlnName_Farm14 = "Kraken.SPFarmLibrary.wsp"
$SlnName_Logging = "Kraken.SPLoggingCategories.wsp"

$SharePointRoot = "C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14"

Write-Host "Including Fucntion Libraries" -ForegroundColor Green
. ./SPDeployFunctions.ps1
Test-SPDeployFunctions
Add-SPSnapIn
Start-AdminService

Write-Host "Copying WSP Packages from Source." -ForegroundColor Green
# Copy-Item -Path ..\$SlnName -Destination .
Copy-Item -Path ..\$SlnName_Farm14 -Destination .
Copy-Item -Path ..\$SlnName_Logging -Destination .

$wspPath = "$(Get-ScriptDirectory)\$SlnName"
$wspPath_Farm14 = "$(Get-ScriptDirectory)\$SlnName_Farm14"
$wspPath_Logging = "$(Get-ScriptDirectory)\$SlnName_Logging"

Write-Host "Checking Solution Dependencies" -ForegroundColor Green
RemoveAdd-Solution -SlnName $SlnName_Farm14 -WspPath $wspPath_Farm14
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

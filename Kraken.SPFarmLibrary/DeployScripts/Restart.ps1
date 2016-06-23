Write-Host "Including Fucntion Libraries" -ForegroundColor Green
. ./SPDeployFunctions.ps1
Test-SPDeployFunctions

## Pre-reqs
Write-Host "Add SharePoint PowerShell Extensions (if needed)" -ForegroundColor Green
$snap = Get-PSSnapin | ? { $_.Name -eq "Microsoft.SharePoint.PowerShell" }
if ($snap -eq $null) {
	Add-PSSnapin Microsoft.SharePoint.PowerShell
}
Write-Host "Start Admin Service (if stopped)" -ForegroundColor Green
Start-AdminService

Write-Host "Resetting IIS Server" -ForegroundColor Green
iisreset

Write-Host "Restarting Timer Job Service" -ForegroundColor Green
Restart-Service "SPTimerV4" ## -displayname "SharePoint 2010 Timer"
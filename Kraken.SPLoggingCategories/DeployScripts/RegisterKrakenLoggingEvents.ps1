$SlnName = "Kraken.SharePoint.wsp"

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

## Deploy (Install) Solution on farm
Write-Host "Deploying Solution" -ForegroundColor Green
Install-SPFarmSolution $SlnName -AllowGACDeployment

## Registering Events
Write-Host "Registering Event Log Categories" -ForegroundColor Green
[System.Reflection.Assembly]::LoadWithPartialName("Kraken.SharePoint")
try {
	[Kraken.SharePoint.Logging.KrakenLoggingService]::Register();
} catch [Exception] {
    Write-Host ".NET Exception!" -ForegroundColor Red
    Write-Host $_.Exception.ToString()
    Write-Host $_.Exception.StackTrace
	if ($_.Exception.InnerException -ne $null) {
		Write-Host "InnerException:" -ForegroundColor Red
		Write-Host $_.Exception.InnerException.ToString()
		Write-Host $_.Exception.InnerException.StackTrace
	}
}
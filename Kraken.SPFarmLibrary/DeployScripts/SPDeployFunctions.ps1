function Test-SPDeployFunctions
{
	Write-Host "Successfully included SPDeployFunctions.ps1" -ForegroundCOlor Yellow
}

function Get-ScriptDirectory
{
  $Invocation = (Get-Variable MyInvocation -Scope 1).Value
  Split-Path $Invocation.MyCommand.Path
}

function Install-SPFarmSolution 
{
param($SolutionName, $targetUrl) 

	$Solution = Get-SPSolution | ? { $_.Name -eq $SolutionName }
	if ($Solution -eq $null) {
		Write-Host "$SolutionName does not exist." -ForegroundCOlor Yellow
	} else { if ($Solution.Deployed -eq $true) {
		Write-Host "$SolutionName already deployed."
	} else {
		Write-Host "Deploying $SolutionName with ID $($Solution.ID)"
		Write-Host "Deploying..." -nonewline
		if ($targetUrl -eq $null -or [string]::IsNullOrEmpty($targetUrl)) {
			Install-SPSolution –Identity $Solution.ID –GACDeployment 
		} else {
			Install-SPSolution –Identity $Solution.ID -WebApplication $targetUrl –GACDeployment 
		}
		while ($Solution.JobExists) {
			Start-Sleep 2
			Write-Host "." -nonewline
		}
		if ($Solution.Deployed -eq $true) {
			Write-Host "Success!"
		} else {
			Write-Host "Fail!"
		}
	}}
}

function Activate-SPFeature
{
param($FeatureID, $targetUrl) 
	$f = Get-SPFeature | ? { $_.ID -eq $FeatureID }
	if ($f -eq $null) {
		Write-Host "Feature with ID $FeatureID does not exist at $targetUrl!" -ForegroundCOlor Yellow
	} else {
		switch ($f.Scope) {
			"Farm" {
				$sLabel = "Farm"
				$target = $null
			}
			"Site" {
				$sLabel = "Site Collection"
				$target = Get-SPSite $targetUrl
			}
			default { ## "Web"
				$sLabel = "Web Site"
				$target = Get-SPWeb $targetUrl
			}
		}
		if ($target -eq $null -and $f.Scope -ne "Farm") {
			Write-Host "$($target.Url) does not exist!" -ForegroundCOlor Yellow
		} else {
			Write-Host "Activating Feature $($f.DisplayName) for $sLabel $targetUrl" -ForegroundCOlor Green
			if ($targetUrl -ne $null) {
				Write-Host "  at $targetUrl" -ForegroundCOlor Green
			}
			switch ($f.Scope) {
				"Farm" {
					$Feature = Get-SPFeature -Farm | ? { $_.ID -eq $FeatureID }
				}
				"Site" {
					$Feature = Get-SPFeature -Site $target | ? { $_.ID -eq $FeatureID }
				}
				default {
					$Feature = Get-SPFeature -Web $target | ? { $_.ID -eq $FeatureID }
				}
			}
			if ($Feature -ne $null) {
				Write-Host "Feature was already Activated."
				Write-Host "De-activating..."
				if ($target -ne $null) {
					Disable-SPFeature –Identity $FeatureID –Url $target.Url
				} else {
					Disable-SPFeature –Identity $FeatureID
				}
			} ## else {
			Write-Host "Activating..."
			if ($target -ne $null) {
				Enable-SPFeature –Identity $FeatureID –Url $target.Url
			} else {
				Enable-SPFeature –Identity $FeatureID
			}
			Write-Host "Done."
			##}	
		}
	}
}

## This function helps to remove and re-add a solution on a multi-server farm.
function RemoveAdd-Solution {
param(
	[string]$SlnName,
	[string]$wspPath,
	[string]$targetUrl = $null
)
	Write-Host "Checking and removing current WSP Package"
	$sln = Get-SPSolution | ? { $_.Name -eq $SlnName }
	if ($sln -ne $null) {
		Write-Host "Removing $SlnName with ID $($sln.Id)"
		Uninstall-SPSolution -Identity $sln.Id

		Write-Host "Retracting" -nonewline
		while ($sln.JobExists) {
			Start-Sleep 2
			Write-Host "." -nonewline
		}
		if ($sln.Deployed -eq $false) {
			Write-Host "Success!"
		} else {
			Write-Host "Fail!" -ForegroundColor Red
		}

		Write-Host "Removing $SlnName with ID $($sln.Id)"
		Remove-SPSolution -Identity $sln.Id
	}
	Write-Host "Adding solution at $wspPath" -ForegroundColor Green
	Add-SPSolution $wspPath

	## Deploy (Install) Solution on farm
	Write-Host "Deploying Solution" -ForegroundColor Green
	Install-SPFarmSolution -SolutionName $SlnName -TargetUrl $targetUrl ## -AllowGACDeployment
}

function Register-SPAlerts {
param(
	[string]$xmlTemplatePath
	,[string]$TargetUrl
	,[int]$minutes
)
	# Note there is no powershell equivalent to updatealerttemplates as of 5/12/2010
	# http://technet.microsoft.com/en-us/library/ff621081.aspx
	Write-Host "Running Required STSADM Commands" -ForegroundColor Green
	Write-Host "Updating Alert Templates..."
	Write-Host "Running command: stsadm -o updatealerttemplates -filename ""$($xmlTemplatePath)"" -url ""$($TargetUrl)"""
	stsadm -o updatealerttemplates -filename "$($xmlTemplatePath)" -url "$($TargetUrl)"

	Write-Host "Checking to make sure Alerts are enabled."
	stsadm -o getproperty -url $TargetUrl -pn alerts-enabled
	Write-Host ""
	stsadm -o getproperty -url $TargetUrl -pn job-immediate-alerts
	Write-Host ""
	##stsadm -o getproperty -url $TargetUrl -pn job-daily-alerts
	##Write-Host ""
	##stsadm -o getproperty -url $TargetUrl -pn job-weekly-alerts
	##Write-Host ""

	Write-Host "Change Alert schedule to 'Every $($minutes) Minutes'"
	## Change this value to less frequent unless the customer requires otheerwise.
	stsadm -o setproperty -url $TargetUrl -pn job-immediate-alerts -pv "every $($minutes) minutes between 0 and 59" 
	## This property is said to have no effect in SP2010.
	##stsadm -o setproperty -url $TargetUrl -pn job-daily-alerts -pv "daily between 0:01:00 and 23:59:00" 

	Write-Host "Disable and re-enable alerts"
	stsadm -o setproperty -url $TargetUrl -pn alerts-enabled -pv "false"
	stsadm -o setproperty -url $TargetUrl -pn alerts-enabled -pv "true"
}

function Add-SPSnapIn {
	Write-Host "Add SharePoint PowerShell Extensions (if needed)" -ForegroundColor Green
	$snap = Get-PSSnapin | ? { $_.Name -eq "Microsoft.SharePoint.PowerShell" }
	if ($snap -eq $null) {
		Add-PSSnapin Microsoft.SharePoint.PowerShell
		Return $true
	}
	Return $false
}

function Start-AdminService {
	Write-Host "Start Admin Service (if stopped)" -ForegroundColor Green
	$AdminServiceName = "SPAdminV4"
	$IsAdminServiceWasRunning = $true;
	if ($(Get-Service $AdminServiceName).Status -eq "Stopped") {
		$IsAdminServiceWasRunning = $false;
		Write-Host 'Starting SharePoint Admin Service...'
		Start-Service $AdminServiceName
		Write-Host 'Done.'
	}
}

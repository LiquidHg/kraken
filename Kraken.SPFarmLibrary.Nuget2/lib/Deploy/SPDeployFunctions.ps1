$global:StatusColor = "Cyan"

function Test-SPDeployFunctions
{
	Write-Host "Successfully included SPDeployFunctions.ps1" -ForegroundCOlor Green
}

function Get-ScriptDirectory
{
  $Invocation = (Get-Variable MyInvocation -Scope 1).Value
  Split-Path $Invocation.MyCommand.Path
}

function Install-SPFarmSolution {
param(
	$SolutionName, 
	$targetUrl,
	[bool]$Force = $false,
	[bool]$Retry = $true
) 
	$Solution = Get-SPSolution | ? { $_.Name -eq $SolutionName }
	if ($Solution -eq $null) {
		Write-Host "$SolutionName does not exist." -ForegroundCOlor Yellow
	} else { if ($Solution.Deployed -eq $true) {
		Write-Host "$SolutionName already deployed."
	} else {
		Write-Host "Deploying $SolutionName with ID $($Solution.ID) scope = '$targetUrl'"
		Write-Host "Deploying..." -nonewline
		try {
			if ($Force) {
				if ([string]::IsNullOrEmpty($targetUrl)) {
					Install-SPSolution –Identity $Solution.ID –GACDeployment -Force
				} else {
					Install-SPSolution –Identity $Solution.ID -WebApplication $targetUrl –GACDeployment -Force
				}
			} else {
				if ($targetUrl -eq $null -or [string]::IsNullOrEmpty($targetUrl)) {
					Install-SPSolution –Identity $Solution.ID –GACDeployment 
				} else {
					Install-SPSolution –Identity $Solution.ID -WebApplication $targetUrl –GACDeployment 
				}
			}
			while ($Solution.JobExists) {
				Start-Sleep 2
				Write-Host "." -nonewline
			}
			if ($Solution.Deployed -eq $true) {
				Write-Host "Success!" -ForegroundColor Green
			} else {
				Write-Host "Fail!" -ForegroundColor Red
				if ($Retry) {
					Write-Host "Retrying, this time with more feeling..." -ForegroundColor Yellow
					Install-SPFarmSolution -SolutionName $SolutionName -targetUrl $targetUrl -Force $true -Retry $false
				} else {
					Write-Host "I give up!!!" -ForegroundColor Red
				}
			}
		} catch {
			Write-Host "An error occured." -ForegroundColor Yellow
			if ($Retry) {
				Write-Host "Retrying, this time with more feeling..." -ForegroundColor Yellow
				Install-SPFarmSolution -SolutionName $SolutionName -targetUrl $targetUrl -Force $true -Retry $false
			} else {
				Write-Host "I give up!!!" -ForegroundColor Red
			}
		}
	}}
}

function Activate-SPFeature {
param(
	[guid]$FeatureID, 
	[string]$targetUrl
) 
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
			Write-Host "Activating Feature $($f.DisplayName) for $sLabel" -ForegroundCOlor $global:StatusColor
			if (-not [string]::IsNullOrEmpty($targetUrl)) {
				Write-Host "  at $targetUrl" -ForegroundCOlor $global:StatusColor
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
					Disable-SPFeature –Identity $FeatureID –Url $target.Url -Confirm:$false
				} else {
					Disable-SPFeature –Identity $FeatureID -Confirm:$false
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
function Remove-Solution {
param(
	[string]$SolutionName,
	[string]$WspPath,
	[string]$TargetUrl = $null,
	[bool]$Retry = $false
)
	Write-Host "Checking and removing current WSP Package" -ForegroundColor $global:StatusColor
	$sln = Get-SPSolution | ? { $_.Name -eq $SolutionName }
	if ($sln -eq $null) {
		Write-Host "$SolutionName is not currently deployed."
	} else {
		Write-Host "Removing $SolutionName with ID $($sln.Id)"
		try {
			if ([string]::IsNullOrEmpty($targetUrl)) {
				Uninstall-SPSolution -Identity $sln.Id -Confirm:$false
			} else {
				Uninstall-SPSolution -Identity $sln.Id -AllWebApplications -Confirm:$false
			}
			Write-Host "Retracting..." -nonewline
			while ($sln.JobExists) {
				Start-Sleep 2
				Write-Host "." -nonewline
			}
			if ($sln.Deployed -eq $false) {
				Write-Host "Success!" -ForegroundColor Green
			} else {
				Write-Host "Fail!" -ForegroundColor Red
				if ($Retry) {
					Write-Host "Retrying failed $SolutionName..." -ForegroundColor Yellow
					Reset-Services -ResetTimerService $true -ResetAdminService $true
					Remove-Solution -SolutionName $SolutionName -wspPath $wspPath -targetUrl $targetUrl -Retry $false
				} else {
					Write-Host "I give up!!!" -ForegroundColor Red
				}
			}
			Write-Host "Removing $SolutionName with ID $($sln.Id)"
			Remove-SPSolution -Identity $sln.Id -Confirm:$false -Force
		} catch {
			Write-Host "An error occured." -ForegroundColor Yellow
			if ($Retry) {
				Write-Host "Retrying failed $SolutionName..." -ForegroundColor Yellow
				Reset-Services -ResetTimerService $true -ResetAdminService $true
				Remove-Solution -SolutionName $SolutionName -wspPath $wspPath -targetUrl $targetUrl -Retry $false
			} else {
				Write-Host "I give up!!!" -ForegroundColor Red
			}
		}
	}
}

function Reset-Services {
param(
	[bool]$ResetIIS = $false,
	[bool]$ResetTimerService = $false,
	[bool]$ResetAdminService = $false
)
	if ($ResetIIS) {
		Write-Host "Restarting IIS" -ForegroundColor $global:StatusColor
		iisreset
	}
	if ($ResetTimerService) {
		Write-Host "Restarting Timer Job Service" -ForegroundColor $global:StatusColor
		Restart-Service "SPTimerV4" ## -displayname "SharePoint 2010 Timer"
	}
	if ($ResetAdminService) {
		Write-Host "Restarting SharePoint Administration Service" -ForegroundColor $global:StatusColor
		Restart-Service "SPAdminV4" ## -displayname "SharePoint 2010 Timer"
	}
}

## This function helps to remove and re-add a solution on a multi-server farm.
function RemoveAdd-Solution {
param(
	[string]$SolutionName
	, [string]$wspPath
	, [string]$targetUrl = $null
	, [bool]$doRemove = $true
	, [bool]$doInstall = $true
)
	Write-Host
	Write-Host "Remove Then Add Solution" -ForegroundColor White
	Write-Host "------------------------" -ForegroundColor White
	if ($doRemove) {
		Remove-Solution -SolutionName $SolutionName -wspPath $wspPath -targetUrl $targetUrl
	}
	if ($doInstall) {
		Write-Host "Adding solution at $wspPath" -ForegroundColor $global:StatusColor
		Add-SPSolution $wspPath
		## Deploy (Install) Solution on farm
		Write-Host "Deploying Solution" -ForegroundColor $global:StatusColor
		Install-SPFarmSolution -SolutionName $SolutionName -TargetUrl $targetUrl # TODO -GACDeployment
	}
	Write-Host "Done."
	Write-Host
}

function Register-SPAlerts {
param(
	[string]$xmlTemplatePath
	,[string]$TargetUrl
	,[int]$minutes
)
	# Note there is no powershell equivalent to updatealerttemplates as of 5/12/2010
	# http://technet.microsoft.com/en-us/library/ff621081.aspx
	Write-Host "Running Required STSADM Commands" -ForegroundColor $global:StatusColor
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
	Write-Host "Add SharePoint PowerShell Extensions (if needed)" -ForegroundColor $global:StatusColor
	$snap = Get-PSSnapin | ? { $_.Name -eq "Microsoft.SharePoint.PowerShell" }
	if ($snap -eq $null) {
		Add-PSSnapin Microsoft.SharePoint.PowerShell
		Return $true
	}
	Return $false
}

function Start-AdminService {
	Write-Host "Start Admin Service (if stopped)" -ForegroundColor $global:StatusColor
	$AdminServiceName = "SPAdminV4"
	$IsAdminServiceWasRunning = $true;
	if ($(Get-Service $AdminServiceName).Status -eq "Stopped") {
		$IsAdminServiceWasRunning = $false;
		Write-Host 'Starting SharePoint Admin Service...'
		Start-Service $AdminServiceName
		Write-Host 'Done.'
	}
}

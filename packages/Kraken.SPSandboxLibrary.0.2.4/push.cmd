REM TODO on new release, update the function below to match the package file name with version
SET NUGET_PUSH_FN=".\bin\Debug\Kraken.SPSandboxLibrary.0.2.4.symbols.nupkg"
nuget push %NUGET_PUSH_FN% -Source "Misc (private)" -ApiKey VSTS
nuget push %NUGET_PUSH_FN% -Source https://api.nuget.org/v3/index.json -ApiKey %NUGET_APIKEY_KRAKEN%
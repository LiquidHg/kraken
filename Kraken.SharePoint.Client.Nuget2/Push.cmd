REM For developers with rights to publish packages to Nuget...
REM TODO on new release, update the function below to match the package file name with version
SET NUGET_PUSH_FN=".\bin\Debug\Kraken.SharePoint.Client.0.2.78.symbols.nupkg"
nuget push %NUGET_PUSH_FN% -Source "Misc (private)" -ApiKey VSTS
nuget push %NUGET_PUSH_FN% -Source https://api.nuget.org/v3/index.json -ApiKey %NUGET_APIKEY_KRAKEN%

REM TODO To push to private VSTS feed, you need get your own upload credentials.
REM The command to add them to Visual Studio will look like this...
REM nuget sources add -name "Misc" -source https://liquidhg.pkgs.visualstudio.com/DefaultCollection/_packaging/Misc/nuget/v2 -username "thomas.carpe" -password ""
REM You should also be able to embed your keys so they aren't needed in the command prompt above
REM nuget setApiKey %VSTS_APIKEY_KRAKEN% -Source https://liquidhg.pkgs.visualstudio.com/DefaultCollection/_packaging/Misc/nuget/v2
REM nuget setApiKey %NUGET_APIKEY_KRAKEN% -Source https://nuget.org/api/v2/

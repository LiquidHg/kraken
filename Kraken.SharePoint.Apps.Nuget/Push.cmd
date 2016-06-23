REM Update the function below to match the package file name
nuget push Kraken.SharePoint.Apps.0.1.6.symbols.nupkg -Source https://liquidhg.pkgs.visualstudio.com/DefaultCollection/_packaging/Misc/nuget/v2 -ApiKey VSTS
REM Hey, you need get your own upload credentials!
REM The command to add them will look like this...
REM nuget sources add -name "Misc" -source https://liquidhg.pkgs.visualstudio.com/DefaultCollection/_packaging/Misc/nuget/v2 -username "thomas.carpe" -password ""
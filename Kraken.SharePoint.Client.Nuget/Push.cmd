REM Update the function below to match the package file name
nuget push Kraken.SharePoint.Client.0.1.43.symbols.nupkg -Source https://liquidhg.pkgs.visualstudio.com/DefaultCollection/_packaging/Misc/nuget/v2 -ApiKey VSTS
nuget push Kraken.SharePoint.Client.0.1.43.symbols.nupkg -Source https://nuget.org/api/v2/ -ApiKey 990b2015-0155-4693-a7c6-7e7a7a570c94
REM Hey, you need get your own upload credentials!
REM The command to add them will look like this...
REM nuget sources add -name "Misc" -source https://liquidhg.pkgs.visualstudio.com/DefaultCollection/_packaging/Misc/nuget/v2 -username "thomas.carpe" -password ""
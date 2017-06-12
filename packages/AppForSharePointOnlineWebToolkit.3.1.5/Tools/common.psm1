# SharePoint references that need to be copy local
$CopyLocalReferences = @(
    @("Microsoft.IdentityModel"),
    @("Microsoft.IdentityModel.Extensions")
)

# Imports needed for VB project
$VbImports = @(
    "Microsoft.SharePoint.Client"
)

Export-ModuleMember -Variable @( 'CopyLocalReferences', 'VbImports' )

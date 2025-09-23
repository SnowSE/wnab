param(
	[Parameter(Mandatory=$true)]
	[ValidateNotNullOrEmpty()]
	[string]$MigrationName
)

# LLM-Dev: MigrationName parameter is now mandatory with basic validation (no default value).
$env:ConnectionStrings__wnabdb = "Server=localhost;Database=wnabdb;User Id=postgres;Password=password;"
dotnet-ef migrations add $MigrationName --project .\src\WNAB.Logic --startup-project .\src\WNAB.API --output-dir Data\Migrations
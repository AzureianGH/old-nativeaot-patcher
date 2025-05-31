# Build all projects in Release configuration

# Check if x86_64-elf-gcc is installed
$gccExists = $null -ne (Get-Command "x86_64-elf-gcc" -ErrorAction SilentlyContinue)
if (-not $gccExists) {
    Write-Host "⚠️ Warning: x86_64-elf-gcc is not found in PATH." -ForegroundColor Yellow
    Write-Host "The GCC.Build package requires x86_64-elf-gcc to be installed for kernel development." -ForegroundColor Yellow
    Write-Host "Please install x86_64-elf-gcc before using C code in kernel projects." -ForegroundColor Yellow
    Write-Host "See src\Cosmos.GCC.Build\README.md for installation instructions." -ForegroundColor Cyan
}

$projects = @(
    'src\Cosmos.API\Cosmos.API.csproj',
    'src\Cosmos.Patcher.Build\Cosmos.Patcher.Build.csproj',
    'src\Cosmos.Patcher\Cosmos.Patcher.csproj',
    'src\Cosmos.Common.Build\Cosmos.Common.Build.csproj',
    'src\Cosmos.Ilc.Build\Cosmos.Ilc.Build.csproj',
    'src\Cosmos.Asm.Build\Cosmos.Asm.Build.csproj',
    'src\Cosmos.GCC.Build\Cosmos.GCC.Build.csproj',
    'src\Cosmos.Patcher.Analyzer.Package\Cosmos.Patcher.Analyzer.Package.csproj',
    'src\Cosmos.Sdk\Cosmos.Sdk.csproj'
)
foreach ($proj in $projects) {
    dotnet build "$PSScriptRoot\$proj" -c Release
}

# Configure the local NuGet source
$sourceName = 'local-packages'
$packagePath = Join-Path $PSScriptRoot 'artifacts\package\release'

# Remove existing source if it already exists to avoid duplication
$existing = dotnet nuget list source | Where-Object { $_ -match $sourceName }
if ($existing) {
    dotnet nuget remove source $sourceName
}

# Add the local source
dotnet nuget add source $packagePath --name $sourceName

# Clear all NuGet caches (HTTP, global packages, temp, and plugins) in one go
dotnet nuget locals all --clear

# Restore project dependencies
dotnet restore

# Uninstall old global Cosmos.Patcher tool if it exists
if (dotnet tool list -g | Select-String '^Cosmos\.Patcher') {
    Write-Host "➖ Uninstalling existing global Cosmos.Patcher tool"
    dotnet tool uninstall -g Cosmos.Patcher
}

# Install the latest global Cosmos.Patcher tool
Write-Host "➕ Installing global Cosmos.Patcher tool"
dotnet tool install -g Cosmos.Patcher --version 1.0.0
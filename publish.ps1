$project = "BulkDDSConverter.csproj"
$output = "publish"
$zipName = "BulkDDSConverter.zip"

Write-Host "Cleaning previous build..."
Remove-Item -Recurse -Force $output -ErrorAction SilentlyContinue
Remove-Item $zipName -ErrorAction SilentlyContinue

Write-Host "Publishing..."
dotnet publish $project -c Release -r win-x64 --self-contained true -o $output

Write-Host "Creating ZIP..."
Compress-Archive -Path "$output\*" -DestinationPath $zipName

Write-Host "Done: $zipName"
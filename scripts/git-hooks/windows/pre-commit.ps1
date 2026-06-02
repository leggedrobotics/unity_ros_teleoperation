Write-Host "Running pre-commit hook: Checking for modified .meta files..."

# Get list of staged .meta files that have been modified
$metaFiles = git diff --cached --name-only --diff-filter=M |
    Select-String -Pattern '\.meta$' |
    ForEach-Object { $_.ToString() }

if ($metaFiles) {

    # Marker file to prevent duplicate runs
    $markerFile = "$env:TEMP/pre-commit-meta-check"

    if (Test-Path $markerFile) {
        Write-Host "Pre-commit hook already ran. Skipping duplicate check."
        exit 2
    }

    Write-Host "Warning: The following .meta files have been modified since the last commit:`n"
    Write-Host $metaFiles
    Write-Host "`nIf this is unintended, please review your changes.`n"

    # Interactive prompt
    $reply = Read-Host "Do you want to continue with the commit? [y/N]"

    if ($reply -notmatch '^[Yy]$') {
        Write-Host "XXXX Commit aborted."
        exit 1
    }

    # Create marker file
    New-Item -ItemType File -Path $markerFile -Force | Out-Null

} else {
    # Remove marker file if no .meta files are staged
    $markerFile = "$env:TEMP/pre-commit-meta-check"
    if (Test-Path $markerFile) {
        Remove-Item $markerFile -Force
    }
}

exit 0

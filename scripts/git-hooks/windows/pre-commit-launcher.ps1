$proc = Start-Process Powershell -ArgumentList "-File .\.git\hooks\pre-commit.ps1" -Wait -PassThru

if ($proc.ExitCode -eq 0) {
    # Commit can proceed
    exit 0
} elseif ($proc.ExitCode -eq 1) {
    Write-Host "Commit aborted by pre-commit hook."
    exit 1
} elseif ($proc.ExitCode -eq 2) {
    Write-Host "Pre-commit hook already ran. Skipping duplicate check."
    exit 0
} else {
    Write-Host "Pre-commit hook failed with unexpected exit code $($proc.ExitCode)."
    exit $($proc.ExitCode)
}

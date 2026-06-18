param (
    [string]$DbUrl = $env:SUPABASE_POSTGRES_CONNECTION_STRING,
    [string]$BackupDir = ".\backups"
)

if ([string]::IsNullOrWhiteSpace($DbUrl)) {
    Write-Host "Error: Missing SUPABASE_POSTGRES_CONNECTION_STRING environment variable." -ForegroundColor Red
    Write-Host "Usage: .\backup_db.ps1 -DbUrl 'postgresql://user:pass@host:5432/db'" -ForegroundColor Yellow
    exit 1
}

if (-Not (Test-Path -Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir | Out-Null
}

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = Join-Path -Path $BackupDir -ChildPath "library_db_backup_$Timestamp.sql"

Write-Host "Starting backup to $BackupFile..." -ForegroundColor Cyan

# Run pg_dump (requires pg_dump to be in PATH)
# Note: For Supabase, it might require specific flags depending on the database size, but standard works for most assignments.
pg_dump --dbname=$DbUrl --no-owner --no-privileges --file=$BackupFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "Backup completed successfully!" -ForegroundColor Green
    
    # Optional: Run the archival process
    Write-Host "Running data archival process..." -ForegroundColor Cyan
    psql --dbname=$DbUrl --command="select archive_old_data();"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Archival completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Archival failed. See error above." -ForegroundColor Red
    }
} else {
    Write-Host "Backup failed. Make sure pg_dump is installed and in your PATH." -ForegroundColor Red
    exit 1
}

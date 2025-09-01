# PowerShell script to execute SQL and add missing Profile columns
# This script connects to the PostgreSQL database and adds the missing columns

param(
    [string]$ConnectionString = "Host=aws-1-eu-central-2.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.vlbfaqnoaxxoehagskxo;Password=EjRSDpGDOqXIL7gW;Timeout=30;CommandTimeout=30;Pooling=true;MinPoolSize=1;MaxPoolSize=20"
)

Write-Host "Connecting to database and adding missing Profile columns..." -ForegroundColor Green

try {
    # Parse connection string
    $connParams = @{}
    $ConnectionString -split ';' | ForEach-Object {
        if ($_ -match '^(.+)=(.+)$') {
            $connParams[$matches[1]] = $matches[2]
        }
    }
    
    # Build psql command
    $psqlCmd = "psql -h $($connParams['Host']) -p $($connParams['Port']) -U $($connParams['Username']) -d $($connParams['Database']) -f add_profile_columns.sql"
    
    Write-Host "Executing: $psqlCmd" -ForegroundColor Yellow
    
    # Set password environment variable for psql
    $env:PGPASSWORD = $connParams['Password']
    
    # Execute the SQL script
    Invoke-Expression $psqlCmd
    
    Write-Host "Profile columns added successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error executing SQL: $_" -ForegroundColor Red
}
finally {
    # Clear password from environment
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}


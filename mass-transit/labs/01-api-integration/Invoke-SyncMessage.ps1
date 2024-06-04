$message = @{
  "type"="Integalactic.Data";
  "value"="A value synced from another galaxy";
  "state"=1
} | ConvertTo-Json

Invoke-WebRequest -Uri http://localhost:5000/api/Sync `
-Method POST `
-Body $message `
-ContentType "application/json"
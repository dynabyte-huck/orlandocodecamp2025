param (
    [string]$apiUrl = "http://localhost:5297/"  # Update with your actual API base URL
)

$subscriberId = [guid]::NewGuid().ToString()
$endpoint = "$apiUrl$subscriberId"

$subscriber = @{
    SubscriberId = $subscriberId
    Email = "jeremy.huckeba@dynabytetech.com"
    FirstName = "Jeremy"
    LastName = "Huckeba"
} | ConvertTo-Json -Depth 10

$headers = @{
    "Content-Type" = "application/json"
}

$response = Invoke-RestMethod -Uri $endpoint -Method Post -Body $subscriber -Headers $headers

Write-Output "Response: $response"
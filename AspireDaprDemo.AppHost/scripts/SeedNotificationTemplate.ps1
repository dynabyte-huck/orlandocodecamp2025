param (
    [string]$apiUrl = "http://localhost:5151/template/",
    [string]$templatePath = "weather_email_template.html",
    [string]$templateId = "CURRENT_WEATHER_1"
)

$endpoint = "$apiUrl$templateId"

if (-Not (Test-Path $templatePath)) {
    Write-Host "Error: Template file not found at $templatePath"
    exit 1
}

$templateContent = Get-Content -Path $templatePath -Raw

$headers = @{
    "Content-Type" = "text/plain"
}

$response = Invoke-RestMethod -Uri $endpoint -Method Post -Body $templateContent -Headers $headers

Write-Output "Response: $response"
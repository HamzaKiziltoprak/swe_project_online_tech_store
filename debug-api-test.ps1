# DEBUG API TEST - Check token and endpoints
$baseUrl = "http://localhost:5100"

Write-Host "=== TOKEN & AUTH DEBUG TEST ===" -ForegroundColor Cyan

# Step 1: Login with custom JWT endpoint
Write-Host "`n1. Login as Admin with JWT..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@example.com"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/accounts/login" -Method POST -Body $loginBody -ContentType "application/json"
    Write-Host "Login Response:" -ForegroundColor Gray
    $loginResponse | ConvertTo-Json -Depth 3
    
    $token = $loginResponse.data.token
    Write-Host "`nJWT Token extracted: $($token.Substring(0, 50))..." -ForegroundColor Green
    
    $headers = @{ 
        Authorization = "Bearer $token"
        Accept = "application/json"
    }
    
    Write-Host "`nHeaders prepared:" -ForegroundColor Gray
    $headers | ConvertTo-Json
    
} catch {
    Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Test Profile Endpoint
Write-Host "`n2. Testing GET /api/accounts/profile..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/accounts/profile" -Method GET -Headers $headers -UseBasicParsing
    Write-Host "SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 3
} catch {
    Write-Host "FAILED - Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "Response Body: $errorBody" -ForegroundColor Red
    }
}

# Step 3: Test Users Endpoint
Write-Host "`n3. Testing GET /api/accounts/users..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/accounts/users" -Method GET -Headers $headers -UseBasicParsing
    Write-Host "SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 3
} catch {
    Write-Host "FAILED - Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Test Product Creation
Write-Host "`n4. Testing POST /api/products..." -ForegroundColor Yellow
$productBody = @{
    productName = "Debug Test Product"
    description = "Testing product creation"
    price = 99.99
    stockQuantity = 10
    categoryId = 1
    brandId = 1
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/products" -Method POST -Body $productBody -ContentType "application/json" -Headers $headers -UseBasicParsing
    Write-Host "SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 3
} catch {
    Write-Host "FAILED - Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "Response Body: $errorBody" -ForegroundColor Red
    }
}

Write-Host "`n=== DEBUG TEST COMPLETE ===" -ForegroundColor Cyan

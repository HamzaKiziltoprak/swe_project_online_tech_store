# DETAILED ENDPOINT TEST - Test problematic endpoints with authentication
$baseUrl = "http://localhost:5102"

Write-Host "=== DETAYLI API TEST ===" -ForegroundColor Cyan
Write-Host ""

# Login and get token
Write-Host "1. Admin Login yapiliyor..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@example.com"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/accounts/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.data.token
    Write-Host "   SUCCESS - Token alindi" -ForegroundColor Green
    $headers = @{ Authorization = "Bearer $token" }
} catch {
    Write-Host "   FAILED - Login yapilamadi: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== AUTHENTICATION GEREKTIREN ENDPOINT'LER ===" -ForegroundColor Cyan
Write-Host ""

$passed = 0
$failed = 0
$tests = @()

# Test function
function Test-Endpoint {
    param($Name, $Method, $Url, $Body = $null, $NeedsAuth = $true)
    
    Write-Host "$Name" -NoNewline
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            UseBasicParsing = $true
        }
        
        if ($NeedsAuth) {
            $params['Headers'] = $script:headers
        }
        
        if ($Body) {
            $params['Body'] = $Body
            $params['ContentType'] = 'application/json'
        }
        
        $response = Invoke-WebRequest @params
        $data = $response.Content | ConvertFrom-Json
        
        Write-Host " - OK" -ForegroundColor Green
        $script:passed++
        $script:tests += [PSCustomObject]@{
            Name = $Name
            Status = "PASS"
            StatusCode = $response.StatusCode
            Message = $data.message
        }
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $errorMsg = $_.Exception.Message
        
        if ($statusCode -eq 404) {
            Write-Host " - 404 (Endpoint bulunamadi)" -ForegroundColor Red
        } elseif ($statusCode -eq 400) {
            Write-Host " - 400 (Bad Request)" -ForegroundColor Red
        } elseif ($statusCode -eq 405) {
            Write-Host " - 405 (Method Not Allowed)" -ForegroundColor Red
        } elseif ($statusCode -eq 401) {
            Write-Host " - 401 (Unauthorized)" -ForegroundColor Red
        } else {
            Write-Host " - ERROR ($statusCode)" -ForegroundColor Red
        }
        
        $script:failed++
        $script:tests += [PSCustomObject]@{
            Name = $Name
            Status = "FAIL"
            StatusCode = $statusCode
            Message = $errorMsg
        }
    }
}

# ACCOUNTS ENDPOINTS
Write-Host "--- ACCOUNTS ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/accounts/profile" "GET" "$baseUrl/api/accounts/profile"
Test-Endpoint "GET /api/accounts/users" "GET" "$baseUrl/api/accounts/users"
Test-Endpoint "GET /api/accounts/roles" "GET" "$baseUrl/api/accounts/roles"
Test-Endpoint "PUT /api/accounts/profile" "PUT" "$baseUrl/api/accounts/profile" (@{firstName="Test"; lastName="Admin"} | ConvertTo-Json)

Write-Host ""
Write-Host "--- PRODUCTS ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/products" "GET" "$baseUrl/api/products" $null $false
Test-Endpoint "GET /api/products/1" "GET" "$baseUrl/api/products/1" $null $false
Test-Endpoint "GET /api/products/search?q=test" "GET" "$baseUrl/api/products/search?q=test" $null $false
Test-Endpoint "GET /api/products/featured" "GET" "$baseUrl/api/products/featured" $null $false
Test-Endpoint "POST /api/products" "POST" "$baseUrl/api/products" (@{
    productName="Test Product"
    description="Test"
    price=99.99
    stockQuantity=10
    categoryId=1
    brandId=1
} | ConvertTo-Json)

Write-Host ""
Write-Host "--- CATEGORIES ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/categories" "GET" "$baseUrl/api/categories" $null $false
Test-Endpoint "GET /api/categories/1" "GET" "$baseUrl/api/categories/1" $null $false
Test-Endpoint "GET /api/categories/1/products" "GET" "$baseUrl/api/categories/1/products" $null $false
Test-Endpoint "GET /api/categories/tree" "GET" "$baseUrl/api/categories/tree" $null $false
Test-Endpoint "POST /api/categories" "POST" "$baseUrl/api/categories" (@{categoryName="Test Category"} | ConvertTo-Json)

Write-Host ""
Write-Host "--- BRANDS ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/brands" "GET" "$baseUrl/api/brands" $null $false
Test-Endpoint "GET /api/brands/1" "GET" "$baseUrl/api/brands/1" $null $false
Test-Endpoint "POST /api/brands" "POST" "$baseUrl/api/brands" (@{brandName="Test Brand"} | ConvertTo-Json)

Write-Host ""
Write-Host "--- CART ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/cart" "GET" "$baseUrl/api/cart"
Test-Endpoint "POST /api/cart/add" "POST" "$baseUrl/api/cart/add" (@{productId=1; quantity=1} | ConvertTo-Json)
Test-Endpoint "DELETE /api/cart" "DELETE" "$baseUrl/api/cart"

Write-Host ""
Write-Host "--- ORDERS ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/orders" "GET" "$baseUrl/api/orders"
Test-Endpoint "POST /api/orders" "POST" "$baseUrl/api/orders" (@{
    shippingAddress="Test Address"
    billingAddress="Test Address"
    paymentMethod="CreditCard"
} | ConvertTo-Json)

Write-Host ""
Write-Host "--- REVIEWS ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/products/1/reviews" "GET" "$baseUrl/api/products/1/reviews" $null $false
Test-Endpoint "GET /api/products/1/reviews/summary" "GET" "$baseUrl/api/products/1/reviews/summary" $null $false

Write-Host ""
Write-Host "--- FAVORITES ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/favorites" "GET" "$baseUrl/api/favorites"
Test-Endpoint "POST /api/favorites" "POST" "$baseUrl/api/favorites" (@{productId=1} | ConvertTo-Json)

Write-Host ""
Write-Host "--- ANALYTICS ENDPOINTS ---" -ForegroundColor Blue
Test-Endpoint "GET /api/analytics/dashboard" "GET" "$baseUrl/api/analytics/dashboard"
Test-Endpoint "GET /api/analytics/top-products" "GET" "$baseUrl/api/analytics/top-products"
Test-Endpoint "GET /api/analytics/revenue" "GET" "$baseUrl/api/analytics/revenue"

Write-Host ""
Write-Host "--- WEATHER ENDPOINT ---" -ForegroundColor Blue
Test-Endpoint "GET /api/weatherforecast" "GET" "$baseUrl/api/weatherforecast" $null $false

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "           TEST SONUCLARI" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Basarili: $passed" -ForegroundColor Green
Write-Host "Basarisiz: $failed" -ForegroundColor Red
Write-Host ""

Write-Host "SORUNLU ENDPOINT'LER:" -ForegroundColor Yellow
$tests | Where-Object { $_.Status -eq "FAIL" } | Format-Table Name, StatusCode, Message -AutoSize

Write-Host ""

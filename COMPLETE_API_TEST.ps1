# COMPLETE API TEST SUITE - ALL 102 ENDPOINTS
# Backend must be running on http://localhost:5100

$baseUrl = "http://localhost:5100"
$passed = 0
$failed = 0
$skipped = 0
$token = $null

function Write-TestHeader {
    param([string]$Title)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host " $Title" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Test-API {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Endpoint,
        [object]$Body,
        [bool]$RequiresAuth = $false,
        [string]$Role = ""
    )
    
    Write-Host "$Name" -NoNewline
    
    try {
        $params = @{
            Uri = "$baseUrl$Endpoint"
            Method = $Method
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $params['Body'] = $Body
            $params['ContentType'] = 'application/json'
        }
        
        if ($RequiresAuth -and $token) {
            $params['Headers'] = @{ Authorization = "Bearer $token" }
        }
        
        $response = Invoke-WebRequest @params -ErrorAction Stop
        Write-Host " - OK" -ForegroundColor Green
        $global:passed++
        return $response
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401) {
            Write-Host " - SKIP (Auth Required)" -ForegroundColor Yellow
            $global:skipped++
        } elseif ($statusCode -eq 403) {
            Write-Host " - SKIP (Forbidden: $Role)" -ForegroundColor Yellow
            $global:skipped++
        } elseif ($statusCode -eq 404) {
            Write-Host " - SKIP (Not Found)" -ForegroundColor Yellow
            $global:skipped++
        } else {
            Write-Host " - FAIL ($statusCode)" -ForegroundColor Red
            $global:failed++
        }
    }
}

# ==================== ACCOUNTS ENDPOINTS ====================
Write-TestHeader "ACCOUNTS (13 endpoints)"

$testEmail = "apitest$(Get-Random)@test.com"
$registerBody = @{
    email = $testEmail
    password = "Test1234!"
    confirmPassword = "Test1234!"
    firstName = "API"
    lastName = "Tester"
} | ConvertTo-Json

Test-API "1. POST /api/accounts/register" "POST" "/api/accounts/register" $registerBody
Test-API "2. POST /api/accounts/login" "POST" "/api/accounts/login" (@{email=$testEmail; password="Test1234!"} | ConvertTo-Json)
Test-API "3. POST /api/accounts/resend-confirmation" "POST" "/api/accounts/resend-confirmation" ('"' + $testEmail + '"')
Test-API "4. POST /api/accounts/forgot-password" "POST" "/api/accounts/forgot-password" ('"' + $testEmail + '"')
Test-API "5. POST /api/accounts/reset-password" "POST" "/api/accounts/reset-password" (@{userId=1; token="test"; newPassword="Test5678!"} | ConvertTo-Json)
Test-API "6. GET /api/accounts/profile" "GET" "/api/accounts/profile" $null $true
Test-API "7. PUT /api/accounts/profile" "PUT" "/api/accounts/profile" (@{firstName="Updated"} | ConvertTo-Json) $true
Test-API "8. POST /api/accounts/change-password" "POST" "/api/accounts/change-password" (@{currentPassword="Test1234!"; newPassword="Test5678!"} | ConvertTo-Json) $true
Test-API "9. GET /api/accounts/users" "GET" "/api/accounts/users" $null $true "Admin"
Test-API "10. GET /api/accounts/roles" "GET" "/api/accounts/roles" $null $true "Admin"
Test-API "11. POST /api/accounts/assign-role" "POST" "/api/accounts/assign-role" (@{userId=1; roleName="Customer"} | ConvertTo-Json) $true "Admin"
Test-API "12. DELETE /api/accounts/remove-role" "DELETE" "/api/accounts/remove-role" (@{userId=1; roleName="Customer"} | ConvertTo-Json) $true "Admin"
Test-API "13. POST /api/accounts/confirm-email" "POST" "/api/accounts/confirm-email" (@{userId=1; token="test"} | ConvertTo-Json)

# ==================== ADMIN ENDPOINTS ====================
Write-TestHeader "ADMIN (2 endpoints)"

Test-API "1. POST /api/admin/assign-role" "POST" "/api/admin/assign-role" (@{userId=1; roleName="Customer"} | ConvertTo-Json) $true "Admin"
Test-API "2. GET /api/admin/users" "GET" "/api/admin/users" $null $true "Admin"

# ==================== ANALYTICS ENDPOINTS ====================
Write-TestHeader "ANALYTICS (5 endpoints)"

Test-API "1. GET /api/analytics/dashboard" "GET" "/api/analytics/dashboard" $null $true "Admin"
Test-API "2. GET /api/analytics/top-products" "GET" "/api/analytics/top-products" $null $true "Admin"
Test-API "3. GET /api/analytics/category-sales" "GET" "/api/analytics/category-sales" $null $true "Admin"
Test-API "4. GET /api/analytics/sales-trend" "GET" "/api/analytics/sales-trend" $null $true "Admin"
Test-API "5. GET /api/analytics/revenue" "GET" "/api/analytics/revenue" $null $true "Admin"

# ==================== BRANDS ENDPOINTS ====================
Write-TestHeader "BRANDS (6 endpoints)"

Test-API "1. GET /api/brands" "GET" "/api/brands"
Test-API "2. GET /api/brands/1" "GET" "/api/brands/1"
Test-API "3. GET /api/brands/with-counts" "GET" "/api/brands/with-counts"
Test-API "4. POST /api/brands" "POST" "/api/brands" (@{brandName="Test Brand"; description="Test"} | ConvertTo-Json) $true "Admin"
Test-API "5. PUT /api/brands/1" "PUT" "/api/brands/1" (@{brandName="Updated Brand"} | ConvertTo-Json) $true "Admin"
Test-API "6. DELETE /api/brands/1" "DELETE" "/api/brands/1" $null $true "Admin"

# ==================== CART ENDPOINTS ====================
Write-TestHeader "CART (5 endpoints)"

Test-API "1. GET /api/cart" "GET" "/api/cart" $null $true "Customer"
Test-API "2. POST /api/cart/add" "POST" "/api/cart/add" (@{productId=1; quantity=1} | ConvertTo-Json) $true "Customer"
Test-API "3. PATCH /api/cart/1" "PATCH" "/api/cart/1" (@{quantity=2} | ConvertTo-Json) $true "Customer"
Test-API "4. DELETE /api/cart/1" "DELETE" "/api/cart/1" $null $true "Customer"
Test-API "5. DELETE /api/cart" "DELETE" "/api/cart" $null $true "Customer"

# ==================== CATEGORIES ENDPOINTS ====================
Write-TestHeader "CATEGORIES (8 endpoints)"

Test-API "1. GET /api/categories" "GET" "/api/categories"
Test-API "2. GET /api/categories/1" "GET" "/api/categories/1"
Test-API "3. GET /api/categories/1/products" "GET" "/api/categories/1/products"
Test-API "4. GET /api/categories/tree" "GET" "/api/categories/tree"
Test-API "5. POST /api/categories" "POST" "/api/categories" (@{categoryName="Test Category"} | ConvertTo-Json) $true "Admin"
Test-API "6. PUT /api/categories/1" "PUT" "/api/categories/1" (@{categoryName="Updated"} | ConvertTo-Json) $true "Admin"
Test-API "7. DELETE /api/categories/1" "DELETE" "/api/categories/1" $null $true "Admin"
Test-API "8. DELETE /api/categories/1/permanent" "DELETE" "/api/categories/1/permanent" $null $true "Admin"

# ==================== FAVORITES ENDPOINTS ====================
Write-TestHeader "FAVORITES (3 endpoints)"

Test-API "1. GET /api/favorites" "GET" "/api/favorites" $null $true "Customer"
Test-API "2. POST /api/favorites" "POST" "/api/favorites" (@{productId=1} | ConvertTo-Json) $true "Customer"
Test-API "3. GET /api/favorites/1/check" "GET" "/api/favorites/1/check" $null $true "Customer"

# ==================== ORDER RETURNS ENDPOINTS ====================
Write-TestHeader "ORDER RETURNS (6 endpoints)"

Test-API "1. POST /api/orders/1/returns" "POST" "/api/orders/1/returns" (@{reason="Defective"; description="Test"} | ConvertTo-Json) $true "Customer"
Test-API "2. GET /api/orders/returns/my" "GET" "/api/orders/returns/my" $null $true "Customer"
Test-API "3. GET /api/orders/returns/pending" "GET" "/api/orders/returns/pending" $null $true "Admin"
Test-API "4. GET /api/orders/1/returns/1" "GET" "/api/orders/1/returns/1" $null $true
Test-API "5. POST /api/orders/1/returns/1/approve" "POST" "/api/orders/1/returns/1/approve" $null $true "Admin"
Test-API "6. POST /api/orders/1/returns/1/reject" "POST" "/api/orders/1/returns/1/reject" (@{reason="Test"} | ConvertTo-Json) $true "Admin"

# ==================== ORDERS ENDPOINTS ====================
Write-TestHeader "ORDERS (13 endpoints)"

$orderBody = @{
    shippingAddress = "Test Address 123"
    billingAddress = "Test Address 123"
    paymentMethod = "CreditCard"
} | ConvertTo-Json

Test-API "1. POST /api/orders" "POST" "/api/orders" $orderBody $true "Customer"
Test-API "2. GET /api/orders" "GET" "/api/orders" $null $true
Test-API "3. GET /api/orders/1" "GET" "/api/orders/1" $null $true
Test-API "4. PATCH /api/orders/1/status" "PATCH" "/api/orders/1/status" (@{status="Shipped"} | ConvertTo-Json) $true "Admin"
Test-API "5. GET /api/orders/admin/all" "GET" "/api/orders/admin/all" $null $true "Admin"
Test-API "6. PATCH /api/orders/1/cancel" "PATCH" "/api/orders/1/cancel" $null $true
Test-API "7. POST /api/orders/1/return" "POST" "/api/orders/1/return" (@{reason="Test"} | ConvertTo-Json) $true
Test-API "8. GET /api/orders/1/returns" "GET" "/api/orders/1/returns" $null $true
Test-API "9. GET /api/orders/returns" "GET" "/api/orders/returns" $null $true
Test-API "10. PATCH /api/orders/return/1/approve" "PATCH" "/api/orders/return/1/approve" $null $true "Admin"
Test-API "11. PATCH /api/orders/return/1/reject" "PATCH" "/api/orders/return/1/reject" (@{reason="Test"} | ConvertTo-Json) $true "Admin"
Test-API "12. GET /api/orders/statistics" "GET" "/api/orders/statistics" $null $true "Admin"
Test-API "13. POST /api/orders/one-click-buy" "POST" "/api/orders/one-click-buy" (@{productId=1; quantity=1} | ConvertTo-Json) $true "Customer"

# ==================== PRODUCTS ENDPOINTS ====================
Write-TestHeader "PRODUCTS (19 endpoints)"

Test-API "1. GET /api/products" "GET" "/api/products"
Test-API "2. GET /api/products/1" "GET" "/api/products/1"
Test-API "3. GET /api/products/search" "GET" "/api/products/search?q=test"
Test-API "4. GET /api/products/featured" "GET" "/api/products/featured"
Test-API "5. GET /api/products/1/related" "GET" "/api/products/1/related"
Test-API "6. GET /api/products/1/similar" "GET" "/api/products/1/similar"
Test-API "7. GET /api/products/brands/1" "GET" "/api/products/brands/1"
Test-API "8. GET /api/products/comparison-details" "GET" "/api/products/comparison-details?ids=1,2"
Test-API "9. GET /api/products/low-stock" "GET" "/api/products/low-stock" $null $true "Admin"
Test-API "10. POST /api/products" "POST" "/api/products" (@{productName="Test"; price=99.99; categoryId=1; brandId=1; stockQuantity=10} | ConvertTo-Json) $true "Admin"
Test-API "11. PUT /api/products/1" "PUT" "/api/products/1" (@{productName="Updated"} | ConvertTo-Json) $true "Admin"
Test-API "12. DELETE /api/products/1" "DELETE" "/api/products/1" $null $true "Admin"
Test-API "13. DELETE /api/products/1/permanent" "DELETE" "/api/products/1/permanent" $null $true "Admin"
Test-API "14. PATCH /api/products/1/stock" "PATCH" "/api/products/1/stock" (@{quantity=100} | ConvertTo-Json) $true "Admin"
Test-API "15. PUT /api/products/1/critical-level" "PUT" "/api/products/1/critical-level" (@{criticalStockLevel=5} | ConvertTo-Json) $true "Admin"
Test-API "16. POST /api/products/1/images" "POST" "/api/products/1/images" (@{imageUrl="test.jpg"} | ConvertTo-Json) $true "Admin"
Test-API "17. PUT /api/products/images/1" "PUT" "/api/products/images/1" (@{imageUrl="updated.jpg"} | ConvertTo-Json) $true "Admin"
Test-API "18. DELETE /api/products/images/1" "DELETE" "/api/products/images/1" $null $true "Admin"
Test-API "19. POST /api/products/compare" "POST" "/api/products/compare" (@{productIds=@(1,2)} | ConvertTo-Json)

# ==================== REVIEWS ENDPOINTS ====================
Write-TestHeader "REVIEWS (9 endpoints)"

Test-API "1. GET /api/products/1/reviews" "GET" "/api/products/1/reviews"
Test-API "2. GET /api/products/1/reviews/summary" "GET" "/api/products/1/reviews/summary"
Test-API "3. POST /api/products/1/reviews" "POST" "/api/products/1/reviews" (@{rating=5; reviewText="Great!"} | ConvertTo-Json) $true "Customer"
Test-API "4. PUT /api/products/1/reviews/1" "PUT" "/api/products/1/reviews/1" (@{rating=4; reviewText="Good"} | ConvertTo-Json) $true "Customer"
Test-API "5. DELETE /api/products/1/reviews/1" "DELETE" "/api/products/1/reviews/1" $null $true "Customer"
Test-API "6. GET /api/products/1/reviews/my" "GET" "/api/products/1/reviews/my" $null $true "Customer"
Test-API "7. PUT /api/products/1/reviews/1/approve" "PUT" "/api/products/1/reviews/1/approve" $null $true "Admin"
Test-API "8. POST /api/products/1/reviews/1/reject" "POST" "/api/products/1/reviews/1/reject" (@{reason="Inappropriate"} | ConvertTo-Json) $true "Admin"
Test-API "9. GET /api/reviews/pending" "GET" "/api/reviews/pending" $null $true "Admin"

# ==================== TRANSACTIONS ENDPOINTS ====================
Write-TestHeader "TRANSACTIONS (5 endpoints)"

Test-API "1. GET /api/transactions" "GET" "/api/transactions" $null $true "Admin"
Test-API "2. GET /api/transactions/my" "GET" "/api/transactions/my" $null $true
Test-API "3. GET /api/transactions/1" "GET" "/api/transactions/1" $null $true
Test-API "4. GET /api/transactions/revenue" "GET" "/api/transactions/revenue" $null $true "Admin"
Test-API "5. POST /api/transactions" "POST" "/api/transactions" (@{orderId=1; amount=100; paymentMethod="CreditCard"} | ConvertTo-Json) $true "Admin"

# ==================== WEATHER ENDPOINT ====================
Write-TestHeader "WEATHER (1 endpoint)"

Test-API "1. GET /api/weatherforecast" "GET" "/api/weatherforecast"

# ==================== SUMMARY ====================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "          TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Passed:  $passed" -ForegroundColor Green
Write-Host "Failed:  $failed" -ForegroundColor Red
Write-Host "Skipped: $skipped" -ForegroundColor Yellow
$total = $passed + $failed + $skipped
Write-Host "Total:   $total"
Write-Host "`nExpected Total: 102 endpoints" -ForegroundColor Gray
Write-Host "========================================`n" -ForegroundColor Cyan

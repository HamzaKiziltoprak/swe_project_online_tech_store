# Online Tech Store – Çalıştırma ve Kurulum

Bu repo iki parçadan oluşur:
- Backend: .NET 8 / ASP.NET Core API (PostgreSQL, JWT, SMTP e-posta)
- Frontend: React + TypeScript (Vite)

## Önkoşullar
- .NET 8 SDK
- Node.js 18+ ve npm
- PostgreSQL erişimi (lokal veya uzak)
- PowerShell (komutlar Windows içindir)

## Ortam Değişkenleri (önerilir)
Varsayılan `appsettings.json` içindeki hassas değerleri doğrudan kullanmak yerine ortam değişkenleriyle geçersiz kılın:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- `EmailSettings__SmtpServer`, `EmailSettings__SmtpPort`, `EmailSettings__SenderEmail`, `EmailSettings__SenderName`, `EmailSettings__Username`, `EmailSettings__Password`, `EmailSettings__EnableSsl`
- `AppUrl` (frontend adresi)

PowerShell örnekleri:
```pwsh
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=online_store;Username=postgres;Password=YOUR_PWD"
$env:Jwt__Key = "your-strong-base64-key"
```

## Backend’i Çalıştırma
1) Gerekli paketleri indir:
```pwsh
dotnet restore Backend/Backend/Backend.csproj
```
2) Gerekirse veritabanı şemasını uygula (EF Core migration’ları):
```pwsh
dotnet ef database update --project Backend/Backend/Backend.csproj
```
3) API’yi başlat:
```pwsh
dotnet run --project Backend/Backend/Backend.csproj
```
- Varsayılan URL: https://localhost:7100 (Kestrel). Swagger: /swagger.

## Backend’i Docker ile Çalıştırma
1) İmajı üret (repo kökünde çalıştır):
```pwsh
docker build -f Backend/Backend/Dockerfile -t online-tech-backend .
```
2) Gerekli ortam değişkenlerini geçirerek konteyneri başlat:
```pwsh
docker run --rm -p 8080:8080 -p 8081:8081 \
	-e ConnectionStrings__DefaultConnection="Host=localhost;Database=online_store;Username=postgres;Password=YOUR_PWD" \
	-e Jwt__Key="your-strong-base64-key" \
	-e Jwt__Issuer="https://localhost:8080" \
	-e Jwt__Audience="https://localhost:8080" \
	-e AppUrl="http://localhost:5173" \
	-e EmailSettings__SmtpServer="smtp.gmail.com" \
	-e EmailSettings__SmtpPort=587 \
	-e EmailSettings__SenderEmail="example@example.com" \
	-e EmailSettings__SenderName="Online Tech Store" \
	-e EmailSettings__Username="example@example.com" \
	-e EmailSettings__Password="smtp_app_password" \
	-e EmailSettings__EnableSsl=true \
	online-tech-backend
```
- Dockerfile 8080/8081 portlarını açıyor; `-p` ile host’a eşleyebilirsiniz.
- Migration gerekirse imajdan tek seferlik çalıştırın: 
```pwsh
docker run --rm \
	-e ConnectionStrings__DefaultConnection="..." \
	online-tech-backend \
	dotnet ef database update --project Backend/Backend/Backend.csproj
```

## Frontend’i Çalıştırma
1) Bağımlılıkları yükle:
```pwsh
cd Frontend
npm install
```
2) Geliştirme sunucusu:
```pwsh
npm run dev
```
- Varsayılan URL: http://localhost:5173

## Testler
- Backend testleri: `dotnet test Tests/Tests.csproj`
- Frontend testleri: `cd Frontend; npm test`

## Faydalı Notlar
- Solution dosyası: OnlineTechStore.sln
- Backend için Dockerfile mevcut (Backend/Backend/Dockerfile).
- Kullanıcı gizli anahtarlarını kaydetmek için `dotnet user-secrets` komutunu (projede UserSecretsId tanımlı) tercih edin.
-- =====================================================
-- NEON DB - ÜRÜN IMPORT SCRIPT
-- =====================================================
-- Bu dosyayı Neon Console -> SQL Editor'a yapıştırın
-- veya psql ile çalıştırın.
--
-- ÖNEMLİ: BrandID ve CategoryID değerlerini
-- kendi veritabanınızdaki mevcut ID'lerle değiştirin!
-- =====================================================

-- Önce mevcut Brand ve Category ID'lerini kontrol edin:
-- SELECT * FROM "Brands";
-- SELECT * FROM "Categories";

-- ÜRÜN EKLEME
INSERT INTO "Products" 
("ProductName", "Description", "BrandID", "CategoryID", "Price", "Stock", "ImageUrl", "IsActive", "CreatedAt", "ViewCount", "CriticalStockLevel", "IsHomeParams")
VALUES 
-- Örnek ürünler (kendi verilerinizle değiştirin)
('Gaming Laptop X1', 'Intel i9, RTX 4090, 32GB RAM, 1TB SSD oyuncu laptop', 1, 1, 45000.00, 15, 'https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=500', true, NOW(), 0, 10, false),
('Wireless Kulaklık Pro', 'Aktif gürültü önleme, 40 saat pil ömrü', 2, 2, 2500.00, 50, 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500', true, NOW(), 0, 10, false),
('Mekanik Klavye RGB', 'Cherry MX Brown switch, RGB aydınlatma', 3, 3, 1800.00, 30, 'https://images.unsplash.com/photo-1618384887929-16ec33fab9ef?w=500', true, NOW(), 0, 10, false),
('4K Monitor 27"', '27 inç IPS panel, 144Hz, HDR destekli', 1, 4, 8500.00, 20, 'https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=500', true, NOW(), 0, 10, false),
('Gaming Mouse Wireless', '25000 DPI, 6 programlanabilir buton', 2, 5, 1200.00, 80, 'https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=500', true, NOW(), 0, 10, false);

-- Başarılı mı kontrol et:
-- SELECT * FROM "Products" ORDER BY "CreatedAt" DESC LIMIT 10;

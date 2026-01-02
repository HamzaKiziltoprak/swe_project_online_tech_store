-- Add ViewCount column to Products table (PostgreSQL/NeonDB)
-- Run this SQL in NeonDB console

ALTER TABLE "Products" ADD COLUMN "ViewCount" INTEGER NOT NULL DEFAULT 0;

-- Optional: Set some initial view counts for testing
-- UPDATE "Products" SET "ViewCount" = floor(random() * 100 + 1)::int;

-- SEEDING 50 COSMETIC PRODUCTS FROM HASAKI WITH DYNAMIC BRAND ID BINDING
-- CategoryId: 1 (Chăm sóc da), 2 (Trang điểm), 3 (Chăm sóc cơ thể)

-- Delete old seed products if any
DELETE FROM ProductBatches WHERE Notes LIKE '%Hasaki Seed%';
DELETE FROM Products WHERE SKU LIKE 'HSK-%';
DELETE FROM Brands WHERE Name IN ('Bioderma', 'La Roche-Posay', 'Paula''s Choice', 'Klairs', 'Skin1004', 'Anessa', 'L''Oréal Paris', 'Vichy', 'Hada Labo', 'Neutrogena', 'Innisfree', 'Some By Mi', 'Estee Lauder', 'Laneige', 'Cosrx');

-- 1. Insert famous cosmetic brands dynamically
INSERT INTO Brands (Name, Country) VALUES 
('Bioderma', 'France'),
('La Roche-Posay', 'France'),
('Paula''s Choice', 'USA'),
('Klairs', 'Korea'),
('Skin1004', 'Korea'),
('Anessa', 'Japan'),
('L''Oréal Paris', 'France'),
('Vichy', 'France'),
('Hada Labo', 'Japan'),
('Neutrogena', 'USA'),
('Innisfree', 'Korea'),
('Some By Mi', 'Korea'),
('Estee Lauder', 'USA'),
('Laneige', 'Korea'),
('Cosrx', 'Korea');

PRINT 'Brands Re-Seeded!';

-- Declare temp table for products to capture IDs
CREATE TABLE #TempProducts (
    Idx INT IDENTITY(1,1),
    Name NVARCHAR(255),
    Price DECIMAL(10,2),
    PromoPrice DECIMAL(10,2),
    Description NVARCHAR(MAX),
    Ingredient NVARCHAR(MAX),
    ImageUrl NVARCHAR(500),
    CategoryId INT,
    BrandName NVARCHAR(100),
    SKU NVARCHAR(100),
    Barcode NVARCHAR(100),
    SkinType NVARCHAR(255),
    IsVegan BIT
);

INSERT INTO #TempProducts (Name, Price, PromoPrice, Description, Ingredient, ImageUrl, CategoryId, BrandName, SKU, Barcode, SkinType, IsVegan)
VALUES
-- Brand: Bioderma
(N'Nước Tẩy Trang Bioderma Sensibio H2O Cho Da Nhạy Cảm 500ml', 425000, 375000, N'Nước tẩy trang Bioderma hồng dịu nhẹ, làm sạch sâu lớp trang điểm bụi bẩn mà không gây khô da.', N'Water (Aqua), PEG-6 Caprylic/Capric Glycerides, Cucumis Sativus (Cucumber) Fruit Extract, Mannitol, Xylitol, Rhamnose.', 'https://file.hstatic.net/1000182747/file/bioderma_sensibio_h2o_500ml_3a059b02ea9a4e8bb247e25fb88b3eb9_1024x1024.jpg', 1, 'Bioderma', 'HSK-BI-001', '3401575645851', N'Da nhạy cảm', 0),
(N'Nước Tẩy Trang Bioderma Sebium H2O Cho Da Dầu Mụn 500ml', 425000, 379000, N'Nước tẩy trang dành riêng cho da dầu, giúp kiềm dầu hiệu quả và làm sạch sâu lỗ chân lông.', N'Water, PEG-6 Caprylic/Capric Glycerides, Sodium Citrate, Zinc Gluconate, Copper Sulfate, Ginkgo Biloba Leaf Extract.', 'https://file.hstatic.net/1000182747/file/bioderma_sebium_h2o_500ml_2b123d4f1a4e8d2e8b15d2a2d5cfc1d1_1024x1024.jpg', 1, 'Bioderma', 'HSK-BI-002', '3401575645790', N'Da dầu, da mụn', 0),
(N'Kem Dưỡng Bioderma Cicabio Crème Phục Hồi Da Tổn Thương 40ml', 345000, 299000, N'Kem dưỡng phục hồi da làm dịu kích ứng, thúc đẩy quá trình tái tạo biểu bì da.', N'Aqua/Water/Eau, Glycerin, Ethylhexyl Palmitate, Fructooligosaccharides, Zinc Oxide, Octyldodecanol.', 'https://file.hstatic.net/1000182747/file/kem_duong_bioderma_cicabio_creme_40ml_a5d898c8c50d4d8e8b15d2a2d5cfc1d_1024x1024.jpg', 1, 'Bioderma', 'HSK-BI-003', '3401353689622', N'Mọi loại da, da tổn thương', 0),

-- Brand: La Roche-Posay
(N'Kem Chống Nắng La Roche-Posay Anthelios UVmune 400 Oil Control Gel-Cream 50ml', 535000, 469000, N'Kem chống nắng kiềm dầu vượt trội bảo vệ da khỏi tia UVA dài nhờ màng lọc Mexoryl 400 độc quyền.', N'Aqua/Water, Diisopropyl Sebacate, Silica, Isopropyl Myristate, Ethylhexyl Salicylate, Bis-Ethylhexyloxyphenol Methoxyphenyl Triazine.', 'https://file.hstatic.net/1000182747/file/la_roche_posay_anthelios_uvmune_400_50ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'La Roche-Posay', 'HSK-LRP-001', '3337875797598', N'Da dầu mụn, nhạy cảm', 0),
(N'Sữa Rửa Mặt La Roche-Posay Effaclar Purifying Foaming Gel 400ml', 565000, 495000, N'Sữa rửa mặt dạng gel giúp làm sạch dầu thừa và bã nhờn, hỗ trợ giảm mụn hiệu quả.', N'Aqua/Water, Sodium Laureth Sulfate, PEG-8, Coco-Betaine, Hexylene Glycol, Sodium Chloride, Zinc PCA.', 'https://file.hstatic.net/1000182747/file/la_roche_posay_effaclar_gel_400ml_a5d898c8c50d4d8e8b15d2a2d5cfc1d_1024x1024.jpg', 1, 'La Roche-Posay', 'HSK-LRP-002', '3337872411992', N'Da dầu, da mụn', 0),
(N'Tinh Chất La Roche-Posay Effaclar Serum Giảm Mụn Đầu Đen & Thâm 30ml', 1045000, 929000, N'Serum chuyên biệt chứa phức hợp 3 acid (Salicylic, Glycolic, LHA) giúp giảm mụn, mờ thâm và thu nhỏ lỗ chân lông.', N'Aqua/Water, Alcohol Denat., Propanediol, Glycolic Acid, Niacinamide, Dimethyl Isosorbide, Salicylic Acid.', 'https://file.hstatic.net/1000182747/file/la_roche_posay_effaclar_serum_30ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'La Roche-Posay', 'HSK-LRP-003', '3337875722828', N'Da mụn, lỗ chân lông to', 0),
(N'Kem Dưỡng La Roche-Posay Cicaplast Baume B5+ Làm Dịu & Phục Hồi Da 40ml', 425000, 365000, N'Kem dưỡng làm dịu kích ứng và phục hồi da chứa Panthenol 5% và Madecassoside.', N'Aqua/Water, Hydrogenated Polyisobutene, Dimethicone, Glycerin, Butyrospermum Parkii Butter/Shea Butter, Panthenol.', 'https://file.hstatic.net/1000182747/file/la_roche_posay_cicaplast_baume_b5_40ml_a5d898c8c50d4d8e8b15d2a2d5cfc1d_1024x1024.jpg', 1, 'La Roche-Posay', 'HSK-LRP-004', '3337875814264', N'Mọi loại da, da kích ứng', 0),

-- Brand: CeraVe
(N'Sữa Rửa Mặt CeraVe Foaming Cleanser Cho Da Dầu 473ml', 430000, 389000, N'Sữa rửa mặt tạo bọt dịu nhẹ bảo vệ hàng rào da với 3 Ceramide thiết yếu và Niacinamide.', N'Aqua/Water, Cocamidopropyl Hydroxysultaine, Glycerin, Sodium Lauroyl Sarcosinate, Ceramides (EOP, AP, NP), Niacinamide.', 'https://file.hstatic.net/1000182747/file/cerave_foaming_facial_cleanser_473ml_a5d898c8c50d4d8e8b15d2a2d5cfc1d_1024x1024.jpg', 1, 'CeraVe', 'HSK-CV-001', '3337875597358', N'Da dầu, da hỗn hợp', 0),
(N'Sữa Rửa Mặt CeraVe Hydrating Cleanser Cho Da Khô 473ml', 430000, 389000, N'Sữa rửa mặt dưỡng ẩm làm sạch nhẹ nhàng không tạo bọt cho làn da khô ráp.', N'Aqua/Water, Glycerin, Cetearyl Alcohol, Phenoxyethanol, Stearyl Alcohol, Cetyl Alcohol, Ceramides (EOP, AP, NP).', 'https://file.hstatic.net/1000182747/file/cerave_hydrating_cleanser_473ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'CeraVe', 'HSK-CV-002', '3337875597198', N'Da khô, da thường', 0),
(N'Kem Dưỡng Ẩm CeraVe Moisturising Cream Cho Da Khô Đến Rất Khô 340g', 450000, 399000, N'Kem dưỡng ẩm chuyên sâu cho mặt và toàn thân phục hồi làn da khô ráp nứt nẻ.', N'Aqua/Water, Glycerin, Cetearyl Alcohol, Caprylic/Capric Triglyceride, Ceramides (EOP, AP, NP), Hyaluronic Acid.', 'https://file.hstatic.net/1000182747/file/cerave_moisturising_cream_340g_a5d898c8c50d4d8e8b15d2a2d5cfc1d_1024x1024.jpg', 1, 'CeraVe', 'HSK-CV-003', '3337875598997', N'Da khô, da rất khô', 0),

-- Brand: Paula's Choice
(N'Dung Dịch Tẩy Tế Bào Chết Paula''s Choice Skin Perfecting 2% BHA Liquid Exfoliant 118ml', 1020000, 899000, N'Sản phẩm tẩy tế bào chết hóa học huyền thoại chứa 2% Salicylic Acid giúp làm sạch lỗ chân lông, trị mụn ẩn.', N'Water (Aqua), Methylpropanediol, Butylene Glycol, Salicylic Acid, Polysorbate 20, Camellia Oleifera (Green Tea) Leaf Extract.', 'https://file.hstatic.net/1000182747/file/paula_s_choice_skin_perfecting_2_bha_118ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Paula''s Choice', 'HSK-PC-001', '792874102006', N'Mọi loại da, da dầu mụn', 0),
(N'Tinh Chất Paula''s Choice Clinical 20% Niacinamide Se Khít Lỗ Chân Lông 20ml', 1890000, 1690000, N'Serum Niacinamide đậm đặc 20% cải thiện tình trạng lỗ chân lông to và da không đều màu.', N'Water (Aqua), Niacinamide, Pentylene Glycol, Butylene Glycol, Glycerin, Acetyl Glucosamine, Ascorbyl Glucoside.', 'https://file.hstatic.net/1000182747/file/paula_s_choice_clinical_20_niacinamide_20ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Paula''s Choice', 'HSK-PC-002', '792874203010', N'Da dầu, lỗ chân lông to', 0),
(N'Tinh Chất Paula''s Choice Clinical 1% Retinol Treatment Trẻ Hóa Da 30ml', 1950000, 1750000, N'Serum Retinol 1% giúp giảm thiểu nếp nhăn sâu, cải thiện độ đàn hồi và làm đều màu da.', N'Water, Dimethicone, Glycerin, Retinol, Tetrahydrodiferuloylmethane, Ceramide NP, Sodium Hyaluronate.', 'https://file.hstatic.net/1000182747/file/paula_s_choice_clinical_1_retinol_30ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Paula''s Choice', 'HSK-PC-003', '792874301013', N'Da lão hóa, nếp nhăn', 0),

-- Brand: Klairs
(N'Nước Hoa Hồng Klairs Supple Preparation Facial Toner Dưỡng Ẩm Da 180ml', 385000, 319000, N'Toner dưỡng ẩm sâu làm dịu da nhạy cảm kích ứng sau khi rửa mặt.', N'Water, Butylene Glycol, Dimethyl Sulfone, Betaine, Caprylic/Capric Triglyceride, Centella Asiatica Extract.', 'https://file.hstatic.net/1000182747/file/klairs_supple_preparation_facial_toner_180ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Klairs', 'HSK-KL-001', '8809115024976', N'Da nhạy cảm, mọi loại da', 1),
(N'Kem Dưỡng Klairs Midnight Blue Calming Cream Làm Dịu Da Ban Đêm 30ml', 390000, 329000, N'Kem dưỡng làm dịu da tức thì, phục hồi da tổn thương sau mụn hoặc điều trị laser.', N'Water, Butylene Glycol, Glycerin, Sodium Hyaluronate, Centella Asiatica Extract, Guaiazulene.', 'https://file.hstatic.net/1000182747/file/klairs_midnight_blue_calming_cream_30ml_a5d898c8c50d4d8e8b15d2a2d5cfc1d_1024x1024.jpg', 1, 'Klairs', 'HSK-KL-002', '8809520261044', N'Da kích ứng, phục hồi sau mụn', 1),
(N'Tinh Chất Klairs Rich Moist Soothing Serum Cấp Nước Sâu 80ml', 395000, 335000, N'Serum cấp nước sâu nuôi dưỡng làn da căng mọng bóng khỏe suốt cả ngày.', N'Water, Sodium Hyaluronate, Centella Asiatica Extract, Beta-Glucan, Glycyrrhiza Glabra (Licorice) Root Extract.', 'https://file.hstatic.net/1000182747/file/klairs_rich_moist_soothing_serum_80ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Klairs', 'HSK-KL-003', '8809115025140', N'Da mất nước, da thường', 1),

-- Brand: Skin1004
(N'Tinh Chất Skin1004 Madagascar Centella Ampoule Chiết Xuất Rau Má 100ml', 470000, 385000, N'Ampoule chứa 100% chiết xuất rau má vùng Madagascar làm dịu da mụn kích ứng nhạy cảm.', N'Centella Asiatica Extract 100%.', 'https://file.hstatic.net/1000182747/file/skin1004_madagascar_centella_ampoule_100ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Skin1004', 'HSK-SK-001', '8809575460546', N'Da mụn, da nhạy cảm', 1),
(N'Kem Dưỡng Skin1004 Madagascar Centella Soothing Cream Làm Dịu Da Mụn 75ml', 380000, 319000, N'Kem dưỡng rau má dạng gel làm dịu tức thì làn da mụn sưng đỏ.', N'Centella Asiatica Extract (72%), Glycerin, Propanediol, Dipropylene Glycol, Ceramides.', 'https://file.hstatic.net/1000182747/file/skin1004_madagascar_centella_soothing_cream_75ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Skin1004', 'HSK-SK-002', '8809575460560', N'Da dầu, da mụn', 1),
(N'Nước Hoa Hồng Skin1004 Madagascar Centella Toning Toner Sáng Da Dịu Nhẹ 210ml', 370000, 299000, N'Toner chiết xuất từ rau má và PHA giúp làm sạch tế bào chết dịu nhẹ, dưỡng ẩm sáng mịn da.', N'Centella Asiatica Extract (84%), Water, Dipropylene Glycol, Niacinamide, Gluconolactone (PHA).', 'https://file.hstatic.net/1000182747/file/skin1004_madagascar_centella_toning_toner_210ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Skin1004', 'HSK-SK-003', '8809575460553', N'Mọi loại da', 1),

-- Brand: Anessa
(N'Kem Chống Nắng Anessa Perfect UV Sunscreen Skincare Milk SPF50+ 60ml', 715000, 599000, N'Sữa chống nắng vật lý lai hóa học bảo vệ da toàn diện chống nước và mồ hôi vượt trội.', N'Dimethicone, Water, Zinc Oxide, Alcohol, Talc, Diisopropyl Sebacate, Ethylhexyl Salicylate.', 'https://file.hstatic.net/1000182747/file/anessa_perfect_uv_sunscreen_skincare_milk_60ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Anessa', 'HSK-AN-001', '4909978120610', N'Mọi loại da, da dầu', 0),
(N'Gel Chống Nắng Anessa Perfect UV Sunscreen Skincare Gel SPF50+ 90g', 575000, 489000, N'Gel chống nắng ẩm mịn mượt mà nâng tông nhẹ tự nhiên thích hợp cho da khô thiếu ẩm.', N'Water, Alcohol, Ethylhexyl Methoxycinnamate, Dimethicone, Salicylate, Glycerin, Silica.', 'https://file.hstatic.net/1000182747/file/anessa_perfect_uv_sunscreen_skincare_gel_90g_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Anessa', 'HSK-AN-002', '4909978120665', N'Da thường, da khô', 0),

-- Brand: L''Oréal Paris
(N'Serum L''Oréal Paris Glycolic-Bright Sáng Da Mờ Thâm Niacinamide 1.0% 30ml', 399000, 329000, N'Serum sáng da mờ thâm chứa phức hợp Glycolic Acid và Niacinamide 1%.', N'Aqua/Water, Niacinamide, Glycerin, Glycolic Acid, Peg-40 Hydrogenated Castor Oil.', 'https://file.hstatic.net/1000182747/file/loreal_paris_glycolic_bright_instant_glowing_serum_30ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'L''Oréal Paris', 'HSK-LO-001', '8994993016488', N'Da không đều màu, thâm sạm', 0),
(N'Nước Tẩy Trang L''Oréal Paris Micellar Water 3-In-1 Deep Cleansing Sạch Sâu 400ml', 219000, 179000, N'Nước tẩy trang 3-in-1 làm sạch sâu lớp trang điểm lâu trôi cứng đầu.', N'Aqua/Water, Cyclopentasiloxane, Isohexadecane, Potassium Phosphate, Sodium Chloride.', 'https://file.hstatic.net/1000182747/file/loreal_paris_3_in_1_micellar_water_deep_cleansing_400ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'L''Oréal Paris', 'HSK-LO-002', '6923701314647', N'Mọi loại da', 0),
(N'Kem Chống Nắng L''Oréal Paris UV Defender Serum Invisible Fluid Mỏng Nhẹ 50ml', 369000, 299000, N'Kem chống nắng dạng sữa mỏng nhẹ bảo vệ da vô hình trước tia UV.', N'Aqua/Water, Alcohol Denat., Diisopropyl Sebacate, Silica, Isopropyl Myristate.', 'https://file.hstatic.net/1000182747/file/loreal_paris_uv_defender_serum_invisible_fluid_50ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'L''Oréal Paris', 'HSK-LO-003', '8994993015481', N'Mọi loại da, da hỗn hợp', 0),

-- Brand: Vichy
(N'Serum Vichy Mineral 89 Booster Dưỡng Chất Phục Hồi & Bảo Vệ Da 50ml', 1020000, 895000, N'Dưỡng chất khoáng khoáng cô đặc Mineral 89 nuôi dưỡng và phục hồi lớp màng bảo vệ da.', N'Aqua/Water, Peg/Ppg/Polybutylene Glycol-8/5/3 Glycerin, Glycerin, Methyl Gluceth-20, Sodium Hyaluronate.', 'https://file.hstatic.net/1000182747/file/vichy_mineral_89_booster_50ml_a5d898c8c50d4d8e8b15d2a2d5cfc1d_1024x1024.jpg', 1, 'Vichy', 'HSK-VI-001', '3337875543249', N'Mọi loại da, da nhạy cảm', 0),
(N'Serum Vichy Liftactiv Specialist B3 Serum Mờ Thâm Sáng Da 30ml', 1050000, 939000, N'Serum ngăn ngừa lão hóa chuyên sâu B3 làm mờ thâm nám tàn nhang hiệu quả.', N'Aqua/Water, Butylene Glycol, Niacinamide, Glycerin, Hydroxyphenoxy Propionic Acid.', 'https://file.hstatic.net/1000182747/file/vichy_liftactiv_specialist_b3_serum_30ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Vichy', 'HSK-VI-002', '3337875791480', N'Da thâm nám, da lão hóa', 0),

-- Brand: Hada Labo
(N'Dung Dịch Dưỡng Ẩm Hada Labo Advanced Nourish Hyaluronic Acid Lotion 170ml', 210000, 169000, N'Lotion dưỡng ẩm sâu với hệ dưỡng ẩm sâu SHA giúp da ẩm mượt căng mịn.', N'Water, Butylene Glycol, Glycerin, Sodium Hyaluronate (HA), Sodium Acetylated Hyaluronate (SHA).', 'https://file.hstatic.net/1000182747/file/hada_labo_advanced_nourish_lotion_170ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Hada Labo', 'HSK-HL-001', '8935006531987', N'Da thường, da khô', 0),
(N'Kem Dưỡng Sáng Da Hada Labo Perfect White Arbutin Cream 50g', 275000, 229000, N'Kem dưỡng trắng da chuyên sâu chứa Arbutin, Vitamin C và B3 giúp làm mờ thâm sạm.', N'Water, White Ichigo, Arbutin, Niacinamide, Sodium Hyaluronate.', 'https://file.hstatic.net/1000182747/file/hada_labo_perfect_white_cream_50g_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Hada Labo', 'HSK-HL-002', '8935006532052', N'Mọi loại da', 0),

-- Brand: Neutrogena
(N'Gel Dưỡng Ẩm Neutrogena Hydro Boost Water Gel Cấp Nước Cho Da Dầu 50g', 420000, 349000, N'Kem dưỡng ẩm dạng gel mát lạnh chứa Hyaluronic Acid cấp nước tức thì cho làn da dầu thiếu nước.', N'Water, Dimethicone, Glycerin, Cetearyl Olivate, Sorbitan Olivate, Sodium Hyaluronate.', 'https://file.hstatic.net/1000182747/file/neutrogena_hydro_boost_water_gel_50g_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Neutrogena', 'HSK-NE-001', '8880005307577', N'Da dầu, da thiếu nước', 0),
(N'Sữa Rửa Mặt Neutrogena Deep Clean Foaming Cleanser 100g', 145000, 119000, N'Sữa rửa mặt tạo bọt làm sạch sâu bã nhờn bụi bẩn cứng đầu sâu trong lỗ chân lông.', N'Water, Stearic Acid, Glycerin, Peg-8, Propylene Glycol, Salicylic Acid.', 'https://file.hstatic.net/1000182747/file/neutrogena_deep_clean_foaming_cleanser_100g_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Neutrogena', 'HSK-NE-002', '4901730095907', N'Da hỗn hợp, da dầu', 0),

-- Brand: Innisfree
(N'Tinh Chất Trà Xanh Innisfree Green Tea Seed Hyaluronic Serum Cấp Ẩm 80ml', 650000, 569000, N'Serum trà xanh nổi tiếng Innisfree cấp ẩm chuyên sâu nuôi dưỡng làn da tươi trẻ.', N'Camellia Sinensis Leaf Extract, Propanediol, Glycerin, Alcohol Denat., Sodium Hyaluronate.', 'https://file.hstatic.net/1000182747/file/innisfree_green_tea_seed_serum_80ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Innisfree', 'HSK-IN-001', '8809612856424', N'Mọi loại da, da thiếu ẩm', 1),
(N'Sữa Rửa Mặt Trà Xanh Innisfree Green Tea Amino Hydrating Cleansing Foam 150g', 260000, 219000, N'Sữa rửa mặt trà xanh tạo bọt bông xốp dịu nhẹ sạch bụi bẩn không khô da.', N'Water, Glycerin, Myristic Acid, Stearic Acid, PEG-32, Potassium Hydroxide, Green Tea Extract.', 'https://file.hstatic.net/1000182747/file/innisfree_green_tea_amino_cleansing_foam_150g_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Innisfree', 'HSK-IN-002', '8809612851993', N'Mọi loại da', 1),
(N'Mặt Nạ Đất Sét Innisfree Super Volcanic Pore Clay Mask 2X 100ml', 380000, 329000, N'Mặt nạn đất sét núi lửa hút sạch dầu thừa, bã nhờn, làm sạch sâu se khít lỗ chân lông.', N'Water, Kaolin, Butylene Glycol, Volcanic Ash, Silica, Bentonite.', 'https://file.hstatic.net/1000182747/file/innisfree_super_volcanic_pore_clay_mask_2x_100ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Innisfree', 'HSK-IN-003', '8809612850989', N'Da dầu, lỗ chân lông to', 1),

-- Brand: Some By Mi
(N'Tinh Chất Some By Mi AHA-BHA-PHA 30 Days Miracle Serum Trị Mụn 50ml', 420000, 339000, N'Serum đặc trị mụn sưng viêm mụn ẩn chứa tràm trà rau má kết hợp AHA-BHA-PHA.', N'Melaleuca Alternifolia (Tea Tree) Leaf Water, Centella Asiatica Extract, AHA, BHA, PHA.', 'https://file.hstatic.net/1000182747/file/some_by_mi_aha_bha_pha_30_days_miracle_serum_50ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Some By Mi', 'HSK-SB-001', '8809647390016', N'Da dầu mụn', 1),
(N'Nước Hoa Hồng Some By Mi AHA-BHA-PHA 30 Days Miracle Toner 150ml', 380000, 299000, N'Toner hỗ trợ giảm mụn làm sạch lỗ chân lông sau 30 ngày sử dụng.', N'Water, Butylene Glycol, Dipropylene Glycol, Glycerin, Niacinamide, Tea Tree Leaf Extract.', 'https://file.hstatic.net/1000182747/file/some_by_mi_aha_bha_pha_30_days_miracle_toner_150ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Some By Mi', 'HSK-SB-002', '8809647390009', N'Da dầu mụn', 1),

-- Brand: Cosrx
(N'Tinh Chất Ốc Sên Cosrx Advanced Snail 96 Mucin Power Essence Phục Hồi 100ml', 390000, 319000, N'Essence chứa 96% dịch nhầy ốc sên cấp ẩm phục hồi da nhạy cảm lão hóa.', N'Snail Secretion Filtrate, Betaine, Butylene Glycol, 1,2-Hexanediol, Sodium Hyaluronate.', 'https://file.hstatic.net/1000182747/file/cosrx_advanced_snail_96_mucin_power_essence_100ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Cosrx', 'HSK-CX-001', '8809416470009', N'Mọi loại da, da tổn thương', 1),
(N'Sữa Rửa Mặt Tràm Trà Cosrx Low pH Good Morning Gel Cleanser Dịu Nhẹ 150ml', 215000, 169000, N'Sữa rửa mặt dạng gel độ pH chuẩn tự nhiên làm sạch sâu dịu da mụn.', N'Water, Cocamidopropyl Betaine, Sodium Lauroyl Methyl Isethionate, Tea Tree Leaf Oil.', 'https://file.hstatic.net/1000182747/file/cosrx_low_ph_good_morning_gel_cleanser_150ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 1, 'Cosrx', 'HSK-CX-002', '8809416470030', N'Da mụn, da nhạy cảm', 1),

-- Category: Trang điểm (CategoryId: 2)
-- L'Oréal Paris
(N'Son Lì L''Oréal Paris Color Riche Intense Volume Matte Sắc Sảo #275', 389000, 329000, N'Son lì mịn môi lâu trôi nâng niu làn môi thời thượng.', N'Dimethicone, Bis-Diglyceryl Polyacyladipate-2, Phenyl Trimethicone, Tridecyl Trimellitate.', 'https://file.hstatic.net/1000182747/file/loreal_paris_color_riche_intense_volume_matte_275_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 2, 'L''Oréal Paris', 'HSK-LO-004', '6923701391945', N'Mọi tông da', 0),
(N'Kem Nền L''Oréal Paris Infallible 24H Fresh Wear Liquid Foundation tông #125', 398000, 339000, N'Kem nền lâu trôi 24 giờ cho lớp nền tự nhiên mịn màng thoáng nhẹ.', N'Aqua/Water, Dimethicone, Isododecane, Alcohol Denat., Ethylhexyl Methoxycinnamate.', 'https://file.hstatic.net/1000182747/file/loreal_paris_infallible_24h_fresh_wear_30ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 2, 'L''Oréal Paris', 'HSK-LO-005', '3600523614455', N'Mọi loại da', 0),
(N'Mascara L''Oréal Paris Lash Paradise Waterproof Dày & Dài Mi Tự Nhiên', 269000, 219000, N'Mascara chống trôi làm dày mi tơi mi quyến rũ không bết dính.', N'Isododecane, Cera Alba / Beeswax, Copernicia Cerifera Cera / Carnauba Wax.', 'https://file.hstatic.net/1000182747/file/loreal_paris_lash_paradise_waterproof_mascara_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 2, 'L''Oréal Paris', 'HSK-LO-006', '30148816', N'Mọi loại mi', 0),

-- Brand: Laneige
(N'Mặt Nạ Ngủ Cho Môi Laneige Lip Sleeping Mask Berry Mùi Quả Mọng 20g', 450000, 385000, N'Mặt nạ ngủ môi giúp tẩy tế bào chết môi cho đôi môi căng mọng mềm mại.', N'Diisostearyl Malate, Hydrogenated Polyisobutene, Phytosteryl/Isostearyl/Cetyl/Stearyl/Behenyl Dimer Dilinoleate.', 'https://file.hstatic.net/1000182747/file/laneige_lip_sleeping_mask_20g_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 2, 'Laneige', 'HSK-LN-001', '8809643069176', N'Mọi loại môi', 0),
(N'Phấn Nước Dưỡng Ẩm Sáng Da Laneige Neo Cushion Glow SPF50+ tông #21N', 650000, 569000, N'Cushion dưỡng sáng nâng tông bóng khỏe tự nhiên chuẩn Hàn Quốc.', N'Water, Titanium Dioxide, Cyclopentasiloxane, Ethylhexyl Methoxycinnamate.', 'https://file.hstatic.net/1000182747/file/laneige_neo_cushion_glow_spf50_15g_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 2, 'Laneige', 'HSK-LN-002', '8809643058866', N'Da khô, da thường', 0),

-- Category: Chăm sóc cơ thể (CategoryId: 3)
-- Cocoon
(N'Tẩy Tế Bào Chết Cơ Thể Cà Phê Đắk Lắk Cocoon Coffee Body Polish 200ml', 125000, 99000, N'Tẩy tế bào chết toàn thân từ cà phê Đắk Lắk nguyên chất kết hợp bơ ca cao làm mịn sáng da.', N'Coffea Arabica (Coffee) Seed Powder, Aqua, Cetearyl Alcohol, Cocos Nucifera (Coconut) Oil, Cocoa Butter.', 'https://file.hstatic.net/1000182747/file/cocoon_dak_lak_coffee_body_polish_200ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'Cocoon', 'HSK-CC-001', '8938500730012', N'Mọi loại da cơ thể', 1),
(N'Nước Dưỡng Tóc Tinh Dầu Bưởi Cocoon Pomelo Hair Tonic 140ml', 145000, 119000, N'Nước dưỡng tóc chứa tinh dầu bưởi giúp ngăn ngừa rụng tóc kích thích mọc tóc dày khỏe.', N'Aqua, Citrus Grandis (Grapefruit) Peel Oil, Panthenol, Tocopheryl Acetate.', 'https://file.hstatic.net/1000182747/file/cocoon_tinh_dau_buoi_pomelo_hair_tonic_140ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'Cocoon', 'HSK-CC-002', '8938500730029', N'Mọi loại tóc, tóc yếu rụng', 1),

-- Brand: Neutrogena
(N'Sữa Dưỡng Thể Neutrogena Body Lotion Light Sesame Formula Mịn Mượt 250ml', 360000, 299000, N'Sữa dưỡng thể hương vừng Sesame thẩm thấu cực nhanh giúp làm mịn sáng da toàn thân.', N'Water, Glycerin, Caprylic/Capric Triglyceride, Sesame Seed Oil, Isopropyl Myristate.', 'https://file.hstatic.net/1000182747/file/neutrogena_body_lotion_light_sesame_formula_250ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'Neutrogena', 'HSK-NE-003', '070501111202', N'Da khô toàn thân', 0),

-- Brand: CeraVe
(N'Sữa Tắm Dưỡng Ẩm CeraVe Hydrating Body Wash 296ml', 370000, 319000, N'Sữa tắm dưỡng ẩm dịu nhẹ cho da nhạy cảm khô ráp bảo vệ hàng rào bảo vệ da.', N'Aqua/Water, Cocamidopropyl Betaine, Glycerin, Sodium Lauroyl Methyl Isethionate, Ceramides.', 'https://file.hstatic.net/1000182747/file/cerave_hydrating_body_wash_296ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'CeraVe', 'HSK-CV-004', '3337875597792', N'Da khô, da nhạy cảm toàn thân', 0),

-- Brand: Paula's Choice
(N'Kem Dưỡng Thể Tẩy Tế Bào Chết Paula''s Choice Resist Weightless 2% BHA Body Treatment 60ml', 399000, 349000, N'Kem dưỡng thể tẩy tế bào chết hóa học trị mụn lưng, dày sừng nang lông hiệu quả.', N'Water, Butylene Glycol, Cetyl Alcohol, Salicylic Acid, Tocopheryl Acetate, Epilobium Angustifolium Extract.', 'https://file.hstatic.net/1000182747/file/paula_s_choice_resist_weightless_2_bha_body_treatment_60ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'Paula''s Choice', 'HSK-PC-004', '792874570014', N'Mọi loại da cơ thể, mụn lưng', 0),

-- Brand: Bioderma
(N'Dầu Tắm Dưỡng Ẩm Làm Dịu Da Bioderma Atoderm Huile de Douche 200ml', 290000, 249000, N'Dầu tắm làm sạch dịu nhẹ giảm ngứa kích ứng dưỡng ẩm suốt 24 giờ cho da nhạy cảm.', N'Aqua/Water/Eau, Glycerin, PEG-7 Glyceryl Cocoate, Sodium Cocoamphoacetate.', 'https://file.hstatic.net/1000182747/file/bioderma_atoderm_huile_de_douche_200ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'Bioderma', 'HSK-BI-004', '3401560016451', N'Da rất khô, da chàm sữa kích ứng', 0),

-- Brand: Vichy
(N'Lăn Khử Mùi Vichy Deodorant Anti-Perspirant 48H Nhãn Đỏ Không Mùi', 260000, 219000, N'Lăn khử mùi chống mồ hôi 48 giờ khô thoáng mát mẻ không gây ố vàng áo.', N'Aqua/Water, Aluminum Chlorohydrate, Aluminum Sesquichlorohydrate, Cetearyl Alcohol.', 'https://file.hstatic.net/1000182747/file/vichy_deodorant_anti_perspirant_48h_50ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'Vichy', 'HSK-VI-003', '3337871310685', N'Mọi loại da vùng nách', 0),

-- Brand: L''Oréal Paris
(N'Dầu Gội L''Oréal Paris Elseve Fall Resist 3X Ngăn Rụng Tóc 620ml', 199000, 169000, N'Dầu gội Elseve ngăn rụng tóc nuôi dưỡng nang tóc khỏe mạnh gấp 3 lần.', N'Aqua/Water, Sodium Laureth Sulfate, Dimethicone, Coco-Betaine, Arginine, Salicylic Acid.', 'https://file.hstatic.net/1000182747/file/loreal_paris_elseve_fall_resist_3x_shampoo_620ml_9cb9df2cf5ab483eb0d2251a2d5cfc1d_1024x1024.jpg', 3, 'L''Oréal Paris', 'HSK-LO-007', '8992304033629', N'Tóc gãy rụng, tóc yếu', 0);


-- 2. Insert into Products table and automatically generate corresponding ProductBatches for FIFO
DECLARE @i INT = 1;
DECLARE @max INT;
SELECT @max = MAX(Idx) FROM #TempProducts;

DECLARE @name NVARCHAR(255), @price DECIMAL(10,2), @promo DECIMAL(10,2), @desc NVARCHAR(MAX), @ingred NVARCHAR(MAX), @img NVARCHAR(500), @catId INT, @brandName NVARCHAR(100), @sku NVARCHAR(100), @barcode NVARCHAR(100), @skin NVARCHAR(255), @vegan BIT;

WHILE @i <= @max
BEGIN
    SELECT 
        @name = Name, @price = Price, @promo = PromoPrice, @desc = Description, @ingred = Ingredient, @img = ImageUrl, @catId = CategoryId, @brandName = BrandName, @sku = SKU, @barcode = Barcode, @skin = SkinType, @vegan = IsVegan
    FROM #TempProducts WHERE Idx = @i;

    -- Lookup Brand ID dynamically
    DECLARE @brandId INT = NULL;
    SELECT @brandId = Id FROM Brands WHERE Name = @brandName;
    
    -- Fallback to 1 (MeiLing) if brand not found
    IF @brandId IS NULL SET @brandId = 1;

    -- Generate Slug from Name
    DECLARE @slug NVARCHAR(255) = LOWER(@name);
    -- Replace spaces and special Vietnamese characters basic replacement
    SET @slug = REPLACE(@slug, ' ', '-');
    SET @slug = REPLACE(@slug, N'á', 'a');
    SET @slug = REPLACE(@slug, N'à', 'a');
    SET @slug = REPLACE(@slug, N'ả', 'a');
    SET @slug = REPLACE(@slug, N'ã', 'a');
    SET @slug = REPLACE(@slug, N'ạ', 'a');
    SET @slug = REPLACE(@slug, N'ă', 'a');
    SET @slug = REPLACE(@slug, N'ắ', 'a');
    SET @slug = REPLACE(@slug, N'ằ', 'a');
    SET @slug = REPLACE(@slug, N'ẳ', 'a');
    SET @slug = REPLACE(@slug, N'ẵ', 'a');
    SET @slug = REPLACE(@slug, N'ặ', 'a');
    SET @slug = REPLACE(@slug, N'â', 'a');
    SET @slug = REPLACE(@slug, N'ấ', 'a');
    SET @slug = REPLACE(@slug, N'ầ', 'a');
    SET @slug = REPLACE(@slug, N'ẩ', 'a');
    SET @slug = REPLACE(@slug, N'ẫ', 'a');
    SET @slug = REPLACE(@slug, N'ậ', 'a');
    SET @slug = REPLACE(@slug, N'é', 'e');
    SET @slug = REPLACE(@slug, N'è', 'e');
    SET @slug = REPLACE(@slug, N'ẻ', 'e');
    SET @slug = REPLACE(@slug, N'ẽ', 'e');
    SET @slug = REPLACE(@slug, N'ẹ', 'e');
    SET @slug = REPLACE(@slug, N'ê', 'e');
    SET @slug = REPLACE(@slug, N'ế', 'e');
    SET @slug = REPLACE(@slug, N'ề', 'e');
    SET @slug = REPLACE(@slug, N'ể', 'e');
    SET @slug = REPLACE(@slug, N'ễ', 'e');
    SET @slug = REPLACE(@slug, N'ệ', 'e');
    SET @slug = REPLACE(@slug, N'í', 'i');
    SET @slug = REPLACE(@slug, N'ì', 'i');
    SET @slug = REPLACE(@slug, N'ỉ', 'i');
    SET @slug = REPLACE(@slug, N'ĩ', 'i');
    SET @slug = REPLACE(@slug, N'ị', 'i');
    SET @slug = REPLACE(@slug, N'ó', 'o');
    SET @slug = REPLACE(@slug, N'ò', 'o');
    SET @slug = REPLACE(@slug, N'ỏ', 'o');
    SET @slug = REPLACE(@slug, N'õ', 'o');
    SET @slug = REPLACE(@slug, N'ọ', 'o');
    SET @slug = REPLACE(@slug, N'ô', 'o');
    SET @slug = REPLACE(@slug, N'ố', 'o');
    SET @slug = REPLACE(@slug, N'ồ', 'o');
    SET @slug = REPLACE(@slug, N'ổ', 'o');
    SET @slug = REPLACE(@slug, N'ỗ', 'o');
    SET @slug = REPLACE(@slug, N'ộ', 'o');
    SET @slug = REPLACE(@slug, N'ơ', 'o');
    SET @slug = REPLACE(@slug, N'ớ', 'o');
    SET @slug = REPLACE(@slug, N'ờ', 'o');
    SET @slug = REPLACE(@slug, N'ở', 'o');
    SET @slug = REPLACE(@slug, N'ỡ', 'o');
    SET @slug = REPLACE(@slug, N'ợ', 'o');
    SET @slug = REPLACE(@slug, N'ú', 'u');
    SET @slug = REPLACE(@slug, N'ù', 'u');
    SET @slug = REPLACE(@slug, N'ủ', 'u');
    SET @slug = REPLACE(@slug, N'ũ', 'u');
    SET @slug = REPLACE(@slug, N'ụ', 'u');
    SET @slug = REPLACE(@slug, N'ư', 'u');
    SET @slug = REPLACE(@slug, N'ứ', 'u');
    SET @slug = REPLACE(@slug, N'ừ', 'u');
    SET @slug = REPLACE(@slug, N'ử', 'u');
    SET @slug = REPLACE(@slug, N'ữ', 'u');
    SET @slug = REPLACE(@slug, N'ự', 'u');
    SET @slug = REPLACE(@slug, 'đ', 'd');
    SET @slug = REPLACE(@slug, N'ý', 'y');
    SET @slug = REPLACE(@slug, N'ỳ', 'y');
    SET @slug = REPLACE(@slug, N'ỷ', 'y');
    SET @slug = REPLACE(@slug, N'ỹ', 'y');
    SET @slug = REPLACE(@slug, N'ỵ', 'y');
    SET @slug = REPLACE(@slug, '(', '');
    SET @slug = REPLACE(@slug, ')', '');
    SET @slug = REPLACE(@slug, '#', '');
    SET @slug = REPLACE(@slug, '+', '');

    -- Insert Product
    INSERT INTO Products (Name, Price, PromoPrice, Description, Ingredient, ImageUrl, CategoryId, BrandId, SKU, Barcode, Slug, SkinType, IsVegan, Stock, IsActive, CreatedAt)
    VALUES (@name, @price, @promo, @desc, @ingred, @img, @catId, @brandId, @sku, @barcode, @slug, @skin, @vegan, 50, 1, GETDATE());

    DECLARE @newProdId INT = SCOPE_IDENTITY();

    -- Create corresponding ProductBatch so that FIFO checkout works immediately
    DECLARE @randomLot NVARCHAR(50) = 'LOT-HSK-' + CAST(100 + @i AS NVARCHAR(10));
    INSERT INTO ProductBatches (ProductId, BatchNumber, ImportQuantity, Quantity, RemainingQuantity, ImportDate, ExpiryDate, ManufactureDate, Location, Notes)
    VALUES (@newProdId, @randomLot, 50, 50, 50, DATEADD(day, -5, GETDATE()), DATEADD(year, 3, GETDATE()), DATEADD(month, -1, GETDATE()), 'Khu C-Tầng 1', 'Hasaki Seed Lot');

    -- Auto set Product.ExpiryDate using the seeded batch
    UPDATE Products SET ExpiryDate = DATEADD(year, 3, GETDATE()) WHERE Id = @newProdId;

    SET @i = @i + 1;
END

DROP TABLE #TempProducts;
PRINT 'Successfully Seeded 50 Hasaki Products!';

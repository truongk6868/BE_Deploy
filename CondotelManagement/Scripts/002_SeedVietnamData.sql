-- Seed data for Locations, Resorts, Condotels in Vietnam
-- Created: 2025-12-19
-- Note: IDs will auto-increment, no need to specify them

-- ============================================
-- 1. INSERT LOCATIONS (ID auto-increment)
-- ============================================

INSERT INTO Locations (Name, Description, ImageUrl)
VALUES 
    ('Hà Nội', 'Thủ đô của Việt Nam, nơi kết hợp giữa phố cổ và hiện đại', 'https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=600'),
    ('TP. Hồ Chí Minh', 'Thành phố lớn nhất Việt Nam với nhiều điểm du lịch hấp dẫn', 'https://images.unsplash.com/photo-1552581234-26160f608093?w=600'),
    ('Đà Nẵng', 'Thành phố du lịch biển đẹp ở miền Trung Việt Nam', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=600'),
    ('Nha Trang', 'Thành phố biển xinh đẹp nổi tiếng với những bãi biển trắng', 'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600'),
    ('Phú Quốc', 'Đảo du lịch lớn nhất Việt Nam với rừng nguyên sinh và biển xanh', 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=600'),
    ('Hạ Long', 'Di sản thế giới với vịnh Hạ Long tuyệt đẹp', 'https://images.unsplash.com/photo-1504681869696-d977211867ff?w=600'),
    ('Huế', 'Thành phố cổ kính, kinh đô cũ của Việt Nam', 'https://images.unsplash.com/photo-1520763185298-1b434c919abe?w=600'),
    ('Sapa', 'Thị trấn vùng cao đẹp mê hoặc với những thửa ruộng bậc thang', 'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600');

-- ============================================
-- 2. INSERT RESORTS (ID auto-increment)
-- ============================================

-- Hà Nội (LocationId = 1)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (1, 'Hanoi Luxury Resort', 'Resort hạng sang trên Hồ Tây', '123 Thanh Niên, Hà Nội'),
    (1, 'Old Quarter Grand Hotel', 'Resort gần phố cổ Hà Nội', '45 Tạ Hiện, Hà Nội');

-- TP. Hồ Chí Minh (LocationId = 2)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (2, 'Saigon Star Resort', 'Resort cao cấp ở trung tâm TP.HCM', '789 Nguyễn Huệ, TPHCM'),
    (2, 'Ben Thanh Heritage Hotel', 'Resort gần chợ Bến Thành', '101 Đồng Khởi, TPHCM');

-- Đà Nẵng (LocationId = 3)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (3, 'Danang Beach Paradise', 'Resort biển đẹp ở Đà Nẵng', 'Bãi biển Mỹ Khê, Đà Nẵng'),
    (3, 'Da Nang Mountain View', 'Resort tại chân Bà Nà Hills', 'Tây Sơn, Đà Nẵng');

-- Nha Trang (LocationId = 4)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (4, 'Nha Trang Seaside Luxury', 'Resort 5 sao ở bãi biển Nha Trang', '1 Trần Phú, Nha Trang'),
    (4, 'Vinpearl Resort Nha Trang', 'Khu nghỉ dưỡng đẳng cấp quốc tế', 'Hòn Tre, Nha Trang');

-- Phú Quốc (LocationId = 5)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (5, 'Phu Quoc Island Resort', 'Resort 5 sao trên đảo Phú Quốc', 'Dương Đông, Phú Quốc'),
    (5, 'Sunset Phu Quoc', 'Resort nhìn ra biển Tây với hoàng hôn đẹp', 'Cửa Cấn, Phú Quốc');

-- Hạ Long (LocationId = 6)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (6, 'Halong Bay Cruise Resort', 'Resort kết hợp cruise trên vịnh Hạ Long', '25 Hạ Long, Quảng Ninh'),
    (6, 'Halong Stellar Resort', 'Resort nhìn ra vịnh Hạ Long', 'Honoi Avenue, Hạ Long');

-- Huế (LocationId = 7)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (7, 'Hue Imperial Palace Hotel', 'Resort gần Thành Huế cổ', '8 Lê Lợi, Huế'),
    (7, 'Hue Riverside Resort', 'Resort trên sông Hương', '12 Lê Lợi, Huế');

-- Sapa (LocationId = 8)
INSERT INTO Resorts (LocationId, Name, Description, Address)
VALUES 
    (8, 'Sapa Mountain Resort', 'Resort tại vùng cao Sapa', '1 Fansipan, Sapa'),
    (8, 'Sapa Stone House', 'Resort vùng cao với view ruộng bậc thang', 'Sapa Town, Lào Cai');

-- ============================================
-- 3. INSERT CONDOTELS (ID auto-increment)
-- ============================================

-- Hanoi Luxury Resort (ResortId = 1)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (1, 'Luxury Villa A', 'Villa sang trọng 3 phòng ngủ với view Hồ Tây', 3, 2, 'Active'),
    (1, 'Deluxe Suite B', 'Phòng suites cao cấp 2 phòng ngủ', 2, 1, 'Active'),
    (1, 'Premium Suite C', 'Phòng hạng nhất 2 phòng ngủ', 2, 1, 'Active');

-- Old Quarter Grand Hotel (ResortId = 2)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (2, 'Heritage Room 101', 'Phòng phong cách cổ 1 phòng ngủ', 1, 1, 'Active'),
    (2, 'Heritage Room 102', 'Phòng phong cách cổ 1 phòng ngủ', 1, 1, 'Active'),
    (2, 'Family Suite 201', 'Phòng gia đình 2 phòng ngủ', 2, 2, 'Active');

-- Saigon Star Resort (ResortId = 3)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (3, 'Star Suite 301', 'Phòng suites 2 phòng ngủ view trung tâm', 2, 2, 'Active'),
    (3, 'Star Suite 302', 'Phòng suites 2 phòng ngủ view trung tâm', 2, 2, 'Active'),
    (3, 'Executive Room 303', 'Phòng hành chính 1 phòng ngủ', 1, 1, 'Active');

-- Ben Thanh Heritage Hotel (ResortId = 4)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (4, 'Classic Room 401', 'Phòng cổ điển 1 phòng ngủ', 1, 1, 'Active'),
    (4, 'Classic Room 402', 'Phòng cổ điển 1 phòng ngủ', 1, 1, 'Active'),
    (4, 'Deluxe Twin 403', 'Phòng deluxe 2 giường đơn', 2, 1, 'Active');

-- Danang Beach Paradise (ResortId = 5)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (5, 'Beachfront Villa A', 'Villa mặt biển 4 phòng ngủ', 4, 3, 'Active'),
    (5, 'Beachfront Suite B', 'Phòng suites mặt biển 2 phòng ngủ', 2, 2, 'Active'),
    (5, 'Ocean View Room C', 'Phòng view biển 1 phòng ngủ', 1, 1, 'Active');

-- Da Nang Mountain View (ResortId = 6)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (6, 'Mountain Villa A', 'Villa view núi Ba Nà 3 phòng ngủ', 3, 2, 'Active'),
    (6, 'Mountain Suite B', 'Phòng suites view cêtre 2 phòng ngủ', 2, 2, 'Active'),
    (6, 'Cozy Room C', 'Phòng ấm cúng 1 phòng ngủ', 1, 1, 'Active');

-- Nha Trang Seaside Luxury (ResortId = 7)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (7, 'Presidential Suite 501', 'Phòng tổng thống mặt biển 3 phòng ngủ', 3, 3, 'Active'),
    (7, 'Luxury Ocean Suite 502', 'Phòng suites sang trọng 2 phòng ngủ', 2, 2, 'Active'),
    (7, 'Deluxe Beachfront 503', 'Phòng deluxe mặt biển 1 phòng ngủ', 1, 1, 'Active');

-- Vinpearl Resort Nha Trang (ResortId = 8)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (8, 'Vinpearl Premier 601', 'Phòng Premier 2 phòng ngủ', 2, 2, 'Active'),
    (8, 'Vinpearl Suite 602', 'Phòng suites 2 phòng ngủ', 2, 2, 'Active'),
    (8, 'Vinpearl Room 603', 'Phòng tiêu chuẩn 1 phòng ngủ', 1, 1, 'Active');

-- Phu Quoc Island Resort (ResortId = 9)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (9, 'Island Villa Platinum', 'Villa đảo hạng bạch kim 4 phòng ngủ', 4, 4, 'Active'),
    (9, 'Island Suite Gold', 'Phòng suites hạng vàng 3 phòng ngủ', 3, 3, 'Active'),
    (9, 'Island Bungalow Silver', 'Nhà tranh hạng bạc 2 phòng ngủ', 2, 2, 'Active');

-- Sunset Phu Quoc (ResortId = 10)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (10, 'Sunset Villa A', 'Villa hoàng hôn 3 phòng ngủ', 3, 2, 'Active'),
    (10, 'Sunset Suite B', 'Phòng suites hoàng hôn 2 phòng ngủ', 2, 2, 'Active'),
    (10, 'Sunset Bungalow C', 'Nhà tranh hoàng hôn 2 phòng ngủ', 2, 1, 'Active');

-- Halong Bay Cruise Resort (ResortId = 11)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (11, 'Cruise Deluxe 701', 'Cabin deluxe trên tàu 2 phòng ngủ', 2, 1, 'Active'),
    (11, 'Cruise Suite 702', 'Cabin suites trên tàu 2 phòng ngủ', 2, 2, 'Active');

-- Halong Stellar Resort (ResortId = 12)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (12, 'Stellar Villa A', 'Villa sao 3 phòng ngủ', 3, 2, 'Active'),
    (12, 'Stellar Suite B', 'Phòng suites sao 2 phòng ngủ', 2, 2, 'Active');

-- Hue Imperial Palace Hotel (ResortId = 13)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (13, 'Imperial Suite 801', 'Phòng suites hoàng gia 2 phòng ngủ', 2, 2, 'Active'),
    (13, 'Palace Room 802', 'Phòng cung điện 1 phòng ngủ', 1, 1, 'Active');

-- Hue Riverside Resort (ResortId = 14)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (14, 'Riverside Villa A', 'Villa sông Hương 3 phòng ngủ', 3, 2, 'Active'),
    (14, 'Riverside Suite B', 'Phòng suites sông Hương 2 phòng ngủ', 2, 2, 'Active');

-- Sapa Mountain Resort (ResortId = 15)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (15, 'Mountain Villa A', 'Villa vùng cao 3 phòng ngủ', 3, 2, 'Active'),
    (15, 'Mountain Suite B', 'Phòng suites vùng cao 2 phòng ngủ', 2, 2, 'Active');

-- Sapa Stone House (ResortId = 16)
INSERT INTO Condotels (ResortId, Name, Description, Beds, Bathrooms, Status)
VALUES 
    (16, 'Stone Villa A', 'Nhà đá vùng cao 3 phòng ngủ', 3, 2, 'Active'),
    (16, 'Stone Room B', 'Phòng đá vùng cao 2 phòng ngủ', 2, 1, 'Active');

-- ============================================
-- 4. INSERT CONDOTEL PRICES (ID auto-increment)
-- ============================================

-- Prices for Hanoi
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (1, 3500000, '2025-01-01', '2025-12-31', 1, 0),
    (2, 2500000, '2025-01-01', '2025-12-31', 1, 0),
    (3, 2000000, '2025-01-01', '2025-12-31', 1, 0);

-- Prices for HCMC
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (4, 1500000, '2025-01-01', '2025-12-31', 1, 0),
    (5, 1500000, '2025-01-01', '2025-12-31', 1, 0),
    (6, 2000000, '2025-01-01', '2025-12-31', 1, 0),
    (7, 2800000, '2025-01-01', '2025-12-31', 1, 0),
    (8, 2200000, '2025-01-01', '2025-12-31', 1, 0),
    (9, 1800000, '2025-01-01', '2025-12-31', 1, 0);

-- Prices for Da Nang
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (10, 1200000, '2025-01-01', '2025-12-31', 1, 0),
    (11, 1200000, '2025-01-01', '2025-12-31', 1, 0),
    (12, 1000000, '2025-01-01', '2025-12-31', 1, 0),
    (13, 4000000, '2025-01-01', '2025-12-31', 1, 0),
    (14, 3000000, '2025-01-01', '2025-12-31', 1, 0),
    (15, 2000000, '2025-01-01', '2025-12-31', 1, 0);

-- Prices for Nha Trang
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (16, 3500000, '2025-01-01', '2025-12-31', 1, 0),
    (17, 2800000, '2025-01-01', '2025-12-31', 1, 0),
    (18, 2200000, '2025-01-01', '2025-12-31', 1, 0),
    (19, 5000000, '2025-01-01', '2025-12-31', 1, 0),
    (20, 4200000, '2025-01-01', '2025-12-31', 1, 0),
    (21, 3200000, '2025-01-01', '2025-12-31', 1, 0);

-- Prices for Phu Quoc
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (22, 3800000, '2025-01-01', '2025-12-31', 1, 0),
    (23, 3200000, '2025-01-01', '2025-12-31', 1, 0),
    (24, 2500000, '2025-01-01', '2025-12-31', 1, 0),
    (25, 6000000, '2025-01-01', '2025-12-31', 1, 0),
    (26, 5000000, '2025-01-01', '2025-12-31', 1, 0),
    (27, 4000000, '2025-01-01', '2025-12-31', 1, 0);

-- Prices for Ha Long
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (28, 3800000, '2025-01-01', '2025-12-31', 1, 0),
    (29, 3200000, '2025-01-01', '2025-12-31', 1, 0),
    (30, 2800000, '2025-01-01', '2025-12-31', 1, 0),
    (31, 3000000, '2025-01-01', '2025-12-31', 1, 0),
    (32, 3500000, '2025-01-01', '2025-12-31', 1, 0);

-- Prices for Hue
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (33, 3000000, '2025-01-01', '2025-12-31', 1, 0),
    (34, 2000000, '2025-01-01', '2025-12-31', 1, 0),
    (35, 2800000, '2025-01-01', '2025-12-31', 1, 0),
    (36, 2200000, '2025-01-01', '2025-12-31', 1, 0);

-- Prices for Sapa
INSERT INTO CondotelPrices (CondotelId, PricePerNight, StartDate, EndDate, IsActive, IsDeleted)
VALUES 
    (37, 2500000, '2025-01-01', '2025-12-31', 1, 0),
    (38, 2000000, '2025-01-01', '2025-12-31', 1, 0),
    (39, 2800000, '2025-01-01', '2025-12-31', 1, 0),
    (40, 2200000, '2025-01-01', '2025-12-31', 1, 0);

PRINT 'Seed data inserted successfully!';
PRINT 'Total Locations: 8';
PRINT 'Total Resorts: 16';
PRINT 'Total Condotels: 42';

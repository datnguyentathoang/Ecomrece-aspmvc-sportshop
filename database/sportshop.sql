CREATE DATABASE SportShop;
GO

USE SportShop;
GO

---------------------------------------------------
-- 0. TÙY CHỌN: Collation (nếu cần) - giữ mặc định
---------------------------------------------------

---------------------------------------------------
-- 1. SECURITY AND USER MANAGEMENT TABLES
---------------------------------------------------

-- Roles
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO

INSERT INTO Roles (RoleName)
VALUES (N'Administrator'), (N'Customer');
GO

-- Users (không chứa Address)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150),
    Email NVARCHAR(150) NOT NULL UNIQUE,
    [Password] NVARCHAR(255) NOT NULL, -- lưu hashed password
    RoleId INT NOT NULL,
    PhoneNumber NVARCHAR(20),
    [Status] BIT DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
);
GO

-- **ĐÃ XÓA: Bảng ApiKeys**
-- **ĐÃ XÓA: Bảng KeyTokens**

---------------------------------------------------
-- 2. PRODUCT MANAGEMENT TABLES
---------------------------------------------------

CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255) NULL
);
GO

CREATE TABLE Brands (
    BrandId INT IDENTITY(1,1) PRIMARY KEY,
    BrandName NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    CategoryId INT NULL,
    BrandId INT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT DEFAULT 0,
    ImageURL NVARCHAR(255) NULL,
    [Description] NVARCHAR(MAX) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId)
        REFERENCES Brands(BrandId)
);
GO

-- Index hữu ích
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_Products_BrandId ON Products(BrandId);
GO

---------------------------------------------------
-- 3. USER ADDRESSES & CART / ORDER TABLES
---------------------------------------------------

-- UserAddresses: 1 user có nhiều địa chỉ
CREATE TABLE UserAddresses (
    AddressId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    AddressLine NVARCHAR(255) NOT NULL, -- ví dụ: "12 Nguyễn Huệ, P. Bến Nghé"
    Ward NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    City NVARCHAR(100) NULL,
    IsDefault BIT DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_UserAddresses_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
);
GO

CREATE INDEX IX_UserAddresses_UserId ON UserAddresses(UserId);

-- Carts (giỏ hàng tạm)
CREATE TABLE Carts (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT DEFAULT 1,

    CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId),
    CONSTRAINT FK_Carts_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);
GO

CREATE INDEX IX_Carts_UserId ON Carts(UserId);

-- Orders (lưu ShippingAddressId trỏ tới UserAddresses)
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT N'Pending',
    ShippingAddressId INT NULL, -- địa chỉ được chọn khi đặt hàng

    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_UserAddresses FOREIGN KEY (ShippingAddressId)
        REFERENCES UserAddresses(AddressId) 
        ON DELETE SET NULL -- nếu address bị xóa, giữ order nhưng null address
);
GO

CREATE INDEX IX_Orders_UserId ON Orders(UserId);
CREATE INDEX IX_Orders_ShippingAddressId ON Orders(ShippingAddressId);

-- OrderDetails
CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);
GO

CREATE INDEX IX_OrderDetails_OrderId ON OrderDetails(OrderId);

-- OrderAddressSnapshot (lưu bản sao địa chỉ tại thời điểm đặt hàng)
CREATE TABLE OrderAddressSnapshot (
    OrderId INT PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    AddressLine NVARCHAR(255) NOT NULL,
    Ward NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    City NVARCHAR(100) NULL,

    CONSTRAINT FK_OrderAddressSnapshot_Orders FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId) ON DELETE CASCADE
);
GO

---------------------------------------------------
-- 4. SAMPLE DATA
---------------------------------------------------

-- Categories
INSERT INTO Categories (CategoryName, [Description])
VALUES 
(N'Sport Shoes', N'Shoes for running, gym, and training...'),
(N'Sportswear', N'Stretchy, cool, moisture-wicking materials'),
(N'Backpacks', N'Convenient for training or travel'),
(N'Gym Accessories', N'Resistance bands, water bottles, gloves...');
GO

-- Brands
INSERT INTO Brands (BrandName)
VALUES (N'Nike'), (N'Adidas'), (N'Puma'), (N'Under Armour');
GO

-- Users (một admin và một customer mẫu)
INSERT INTO Users (FullName, Email, [Password], RoleId, PhoneNumber)
VALUES 
(N'Admin System', 'admin@sportshop.local', 'HASHED_PASSWORD_ADMIN', 1, '0900000000'),
(N'Nguyễn Văn A', 'nguyenvana@example.com', 'HASHED_PASSWORD_USER', 2, '0901234567');
GO

-- **ĐÃ XÓA: INSERT INTO KeyTokens (ví dụ)**

-- Products
INSERT INTO Products (ProductName, CategoryId, BrandId, Price, StockQuantity, [Description], ImageURL)
VALUES 
(N'Nike Air Zoom Shoes', 1, 1, 2500000, 20, N'Premium running shoes', 'nike_air_zoom.jpg'),
(N'Adidas Men Sport Shirt', 2, 2, 550000, 50, N'Good sweat absorption', 'adidas_shirt.jpg'),
(N'Puma Gym Backpack', 3, 3, 450000, 30, N'Durable, waterproof design', 'puma_bag.jpg');
GO

-- UserAddresses cho user 2 (Nguyễn Văn A)
INSERT INTO UserAddresses (UserId, FullName, PhoneNumber, AddressLine, Ward, District, City, IsDefault)
VALUES
(2, N'Nguyễn Văn A', '0901234567', N'12 Nguyễn Huệ', N'Bến Nghé', N'Quận 1', N'TP.HCM', 1),
(2, N'Nguyễn Văn A', '0901234567', N'22 Hoàng Diệu', N'Phường 10', N'Quận 4', N'TP.HCM', 0);
GO

-- Ví dụ đặt hàng: user 2 chọn addressId = 1
INSERT INTO Orders (UserId, TotalAmount, ShippingAddressId)
VALUES (2, 750000, 1);
GO

-- Lấy OrderId vừa tạo (nếu cần, trên môi trường script bạn có thể dùng SCOPE_IDENTITY)
-- Giả sử OrderId = 1, thêm chi tiết đơn hàng
INSERT INTO OrderDetails (OrderId, ProductId, Quantity, UnitPrice)
VALUES (1, 1, 1, 2500000), (1, 3, 1, 450000);
GO

-- Copy address snapshot for that order (nên thực hiện trong transaction ở code khi tạo order)
INSERT INTO OrderAddressSnapshot (OrderId, FullName, PhoneNumber, AddressLine, Ward, District, City)
SELECT TOP 1 o.OrderId, ua.FullName, ua.PhoneNumber, ua.AddressLine, ua.Ward, ua.District, ua.City
FROM Orders o
JOIN UserAddresses ua ON o.ShippingAddressId = ua.AddressId
WHERE o.OrderId = 1;
GO

-- **ĐÃ XÓA: ApiKeys sample**

---------------------------------------------------
-- 5. GỢI Ý VỀ IMPLEMENTATION (ở phía ứng dụng)
---------------------------------------------------
/*
- Khi user đặt hàng:
  1) Chọn 1 AddressId từ UserAddresses (hoặc nhập địa chỉ mới -> tạo UserAddresses mới).
  2) Tạo record trong Orders (lưu ShippingAddressId).
  3) Tạo OrderDetails.
  4) Copy toàn bộ thông tin địa chỉ vào OrderAddressSnapshot (để lịch sử bất biến).
  ==> Thực hiện trong TRANSACTION để đảm bảo consistency.

- Nếu muốn xóa địa chỉ (UserAddresses): 
  - Do Orders.ShippingAddressId có ON DELETE SET NULL, các order cũ không mất, nhưng bạn đã có snapshot để hiển thị.
- Có thể thêm trigger hoặc logic để đảm bảo chỉ 1 address có IsDefault = 1 cho mỗi user.
*/

---------------------------------------------------
-- END

-- Create Database
CREATE DATABASE SportShop;
GO
USE SportShop;
GO

---------------------------------------------------
-- 1. SECURITY AND USER MANAGEMENT TABLES
---------------------------------------------------

-- Role Table (VaiTro)
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
INSERT INTO Roles (RoleName)
VALUES (N'Administrator'), (N'Customer');
GO

-- API Keys Table (ApiKeys)
CREATE TABLE ApiKeys (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(255) NOT NULL UNIQUE,
    [Status] BIT DEFAULT 1,
    [Permissions] NVARCHAR(50) NOT NULL, -- Storing JSON string
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
GO

-- User Table (NguoiDung)
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150),
    Email NVARCHAR(150) NOT NULL UNIQUE,
    [Password] NVARCHAR(255) NOT NULL, -- Hashed Password (e.g., BCrypt)
    RoleId INT NOT NULL,
    PhoneNumber NVARCHAR(20),
    [Address] NVARCHAR(255),
    [Status] BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
);
GO

-- Key Token Model Table (KhoaBaoMatToken)
CREATE TABLE KeyTokens (
    KeyTokenId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL UNIQUE,          -- Links to the user

    PrivateKey NVARCHAR(MAX) NOT NULL,   -- Private Key for JWT signing
    PublicKey NVARCHAR(MAX) NOT NULL,     -- Public Key for JWT verification
   
    -- List of used Refresh Tokens (for Replay Attack detection, stored as JSON array)
    RefreshTokensUsed NVARCHAR(MAX) DEFAULT N'[]',

    -- The current active Refresh Token
    CurrentRefreshToken NVARCHAR(MAX) NOT NULL,
   
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_KeyTokens_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
);
GO

---------------------------------------------------
-- 2. PRODUCT MANAGEMENT TABLES
---------------------------------------------------

-- Category Table (DanhMuc)
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(255)
);
GO

-- Brand Table (ThuongHieu)
CREATE TABLE Brands (
    BrandId INT IDENTITY(1,1) PRIMARY KEY,
    BrandName NVARCHAR(100) NOT NULL
);
GO

-- Product Table (SanPham)
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    CategoryId INT,
    BrandId INT,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT DEFAULT 0,
    ImageURL NVARCHAR(255),
    [Description] NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId)
        REFERENCES Brands(BrandId)
);
GO

---------------------------------------------------
-- 3. E-COMMERCE TRANSACTION TABLES
---------------------------------------------------

-- Shopping Cart Table (GioHang)
CREATE TABLE Carts (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    ProductId INT,
    Quantity INT DEFAULT 1,

    CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId),
    CONSTRAINT FK_Carts_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);
GO

-- Order Table (DonHang)
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    OrderDate DATETIME DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(50) DEFAULT N'Pending', -- N'Chờ xử lý'

    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
);
GO

-- Order Detail Table (ChiTietDonHang)
CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT,
    ProductId INT,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId),
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);
GO

---------------------------------------------------
-- 4. SAMPLE DATA
---------------------------------------------------

-- Sample Data for Categories
INSERT INTO Categories (CategoryName, [Description])
VALUES 
(N'Sport Shoes', N'Shoes for running, gym, and training...'),
(N'Sportswear', N'Stretchy, cool, moisture-wicking materials'),
(N'Backpacks', N'Convenient for training or travel'),
(N'Gym Accessories', N'Resistance bands, water bottles, gloves...');

-- Sample Data for Brands
INSERT INTO Brands (BrandName)
VALUES (N'Nike'), (N'Adidas'), (N'Puma'), (N'Under Armour');

-- Sample Data for Products
INSERT INTO Products (ProductName, CategoryId, BrandId, Price, StockQuantity, [Description], ImageURL)
VALUES 
(N'Nike Air Zoom Shoes', 1, 1, 2500000, 20, N'Premium running shoes', 'nike_air_zoom.jpg'),
(N'Adidas Men Sport Shirt', 2, 2, 550000, 50, N'Good sweat absorption', 'adidas_shirt.jpg'),
(N'Puma Gym Backpack', 3, 3, 450000, 30, N'Durable, waterproof design', 'puma_bag.jpg');
GO

-- Sample Data for ApiKeys
INSERT INTO ApiKeys ([Key], [Status], [Permissions])
VALUES (
  'df1fd93e760ad6c176cb520c320a9f6f39d10383f51a2566d66c3c6fbe0476613abd7aa61eaad07f492b5901abff379d84d9180b394aefdb55a7e0f91ea39214',
  1,
  '["0000","1111"]'
);
GO
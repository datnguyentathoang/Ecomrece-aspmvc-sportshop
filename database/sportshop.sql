-- Tạo cơ sở dữ liệu
CREATE DATABASE SportShop;
GO
USE SportShop;
GO

-- Bảng phân quyền (vai trò người dùng)
CREATE TABLE VaiTro (
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL UNIQUE
);
INSERT INTO VaiTro (TenVaiTro)
VALUES (N'Quản trị viên'), (N'Khách hàng');
GO

-- Bảng người dùng
CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(150),
    Email NVARCHAR(150) NOT NULL UNIQUE,
    MatKhau NVARCHAR(255) NOT NULL, -- Mã hoá (BCrypt)
    MaVaiTro INT NOT NULL FOREIGN KEY REFERENCES VaiTro(MaVaiTro),
    SoDienThoai NVARCHAR(20),
    DiaChi NVARCHAR(255),
    TrangThai BIT DEFAULT 1,
    NgayTao DATETIME DEFAULT GETDATE(),
    
    -- *** CỘT MỚI ĐÃ THÊM ĐỂ LƯU KHÓA CÔNG KHAI RSA ***
    -- NVARCHAR(MAX) để đảm bảo đủ chỗ cho chuỗi XML của Public Key (khoảng 1000-2000 ký tự)
    PublicKey NVARCHAR(MAX) NULL 
);
GO

-- Bảng danh mục sản phẩm
CREATE TABLE DanhMuc (
    MaDanhMuc INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(255)
);
GO

-- Bảng thương hiệu
CREATE TABLE ThuongHieu (
    MaThuongHieu INT IDENTITY(1,1) PRIMARY KEY,
    TenThuongHieu NVARCHAR(100) NOT NULL
);
GO

-- Bảng sản phẩm
CREATE TABLE SanPham (
    MaSanPham INT IDENTITY(1,1) PRIMARY KEY,
    TenSanPham NVARCHAR(150) NOT NULL,
    MaDanhMuc INT FOREIGN KEY REFERENCES DanhMuc(MaDanhMuc),
    MaThuongHieu INT FOREIGN KEY REFERENCES ThuongHieu(MaThuongHieu),
    Gia DECIMAL(18,2) NOT NULL,
    SoLuongTon INT DEFAULT 0,
    HinhAnh NVARCHAR(255),
    MoTa NVARCHAR(MAX),
    NgayTao DATETIME DEFAULT GETDATE()
);
GO

-- Bảng giỏ hàng
CREATE TABLE GioHang (
    MaGioHang INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung),
    MaSanPham INT FOREIGN KEY REFERENCES SanPham(MaSanPham),
    SoLuong INT DEFAULT 1
);
GO

-- Bảng đơn hàng
CREATE TABLE DonHang (
    MaDonHang INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung),
    NgayDat DATETIME DEFAULT GETDATE(),
    TongTien DECIMAL(18,2) NOT NULL,
    TrangThai NVARCHAR(50) DEFAULT N'Chờ xử lý'
);
GO

-- Bảng chi tiết đơn hàng
CREATE TABLE ChiTietDonHang (
    MaChiTiet INT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang INT FOREIGN KEY REFERENCES DonHang(MaDonHang),
    MaSanPham INT FOREIGN KEY REFERENCES SanPham(MaSanPham),
    SoLuong INT NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL
);
GO

-- Bảng Refresh Token cho JWT (lưu đăng nhập lâu dài)
CREATE TABLE TheLamMoiToken (
    MaToken INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT FOREIGN KEY REFERENCES NguoiDung(MaNguoiDung),
    Token NVARCHAR(max) NOT NULL,
    HetHanVao DATETIME NOT NULL,
    DaHuy BIT DEFAULT 0,
    NgayTao DATETIME DEFAULT GETDATE()
);
GO

-- Dữ liệu mẫu
INSERT INTO DanhMuc (TenDanhMuc, MoTa)
VALUES 
(N'Giày thể thao', N'Sản phẩm giày dùng cho chạy bộ, tập gym...'),
(N'Áo thể thao', N'Chất liệu co giãn, thoáng mát'),
(N'Balo', N'Tiện dụng khi tập luyện hoặc đi chơi'),
(N'Phụ kiện gym', N'Dây kháng lực, bình nước, găng tay...');

INSERT INTO ThuongHieu (TenThuongHieu)
VALUES (N'Nike'), (N'Adidas'), (N'Puma'), (N'Under Armour');

INSERT INTO SanPham (TenSanPham, MaDanhMuc, MaThuongHieu, Gia, SoLuongTon, MoTa, HinhAnh)
VALUES 
(N'Giày Nike Air Zoom', 1, 1, 2500000, 20, N'Giày chạy bộ cao cấp', 'nike_air_zoom.jpg'),
(N'Áo thể thao Adidas nam', 2, 2, 550000, 50, N'Thấm hút mồ hôi tốt', 'adidas_shirt.jpg'),
(N'Balo Puma tập gym', 3, 3, 450000, 30, N'Thiết kế bền bỉ, chống thấm', 'puma_bag.jpg');
GO
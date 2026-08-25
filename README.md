# ScanAndRemoveVirus

Ứng dụng mô phỏng giao diện phần mềm diệt virus trên Windows, viết bằng **C# WinForms (.NET Framework 4.7.2)**.

> ⚠️ **Lưu ý:** Ứng dụng đã có engine quét thật (quét song song theo chữ ký thử nghiệm `XVIRUS-TEST-SIGNATURE::` + nhận diện tên tệp chứa "eicar", cách ly/xóa được tệp) nhưng chưa phải engine diệt virus chuyên nghiệp. Dữ liệu ở tab Lịch sử, Cách ly, Bảo vệ vẫn là dữ liệu mẫu.

## Tính năng giao diện

| Màn hình | UserControl | Nội dung |
|----------|-------------|----------|
| Tổng quan | `UcTongQuan` | Trạng thái bảo vệ, quét hệ thống, thống kê và khu vực **Hành động** (cách ly/xóa tệp phát hiện sau quét) |
| Bảo vệ | `UcBaoVe` | Danh sách các tính năng bảo vệ kèm nút Bật/Tắt |
| Lịch sử | `UcLichSu` | Lịch sử các lần quét (loại quét, phạm vi, kết quả, thời gian) |
| Cách ly | `UcCachLy` | Danh sách tệp đã cách ly + tổng số tệp |

## Cấu trúc project

```
ScanAndRemoveVirus/
├── Program.cs              # Điểm vào ứng dụng → khởi chạy FrmMain
├── FrmMain.cs              # Cửa sổ chính: sidebar điều hướng + pnlContent
├── Control/                # Các UserControl (nội dung từng tab)
│   ├── UcTongQuan.cs
│   ├── UcBaoVe.cs
│   ├── UcLichSu.cs
│   └── UcCachLy.cs
├── form/                   # Form phụ (hiện chưa được sử dụng)
│   ├── FrmBaoVe.cs
│   └── FrmCachLy.cs
└── Properties/
```

**Cơ chế điều hướng:** `FrmMain` giữ 4 UserControl dùng chung, khi bấm nút sidebar thì `LoadContent()` xóa `pnlContent` và đổ UserControl tương ứng vào (`FrmMain.cs`). Nút đang chọn được highlight qua `ActiveSidebar()` / `ResetSidebar()`.

## Yêu cầu

- Windows 10/11 (có sẵn .NET Framework 4.7.2)
- Visual Studio 2019/2022 với workload **.NET desktop development**

## Chạy project

### Cách 1: Visual Studio
1. Mở `ScanAndRemoveVirus\ScanAndRemoveVirus\ScanAndRemoveVirus.csproj`
2. Nhấn **F5** hoặc **Ctrl+F5**

### Cách 2: Dòng lệnh
```powershell
# Build (msbuild nằm trong Developer Command Prompt for VS)
msbuild ScanAndRemoveVirus\ScanAndRemoveVirus\ScanAndRemoveVirus.csproj /p:Configuration=Debug

# Chạy
ScanAndRemoveVirus\ScanAndRemoveVirus\bin\Debug\ScanAndRemoveVirus.exe
```

Hoặc chạy thẳng bản build có sẵn:
```
ScanAndRemoveVirus\ScanAndRemoveVirus\bin\Debug\ScanAndRemoveVirus.exe
```

## Hướng phát triển tiếp theo

- Nâng cấp bảng chữ ký thật / tích hợp engine quét chính thức thay cho chữ ký thử nghiệm
- Lưu lịch sử quét vào file hoặc database thay vì gán cứng
- Đồng bộ danh sách cách ly thật (%AppData%\ScanAndRemoveVirus\Quarantine) với tab Cách ly (hiện vẫn là dữ liệu mẫu)
- Xử lý sự kiện nút Bật/Tắt ở màn hình Bảo vệ (hiện đang trống)
- Dọn dẹp: xóa using trùng lặp ở `FrmMain.cs:11`, các form không dùng (`Form1`, `FrmBaoVe`, `FrmCachLy`)
- Thêm `.gitignore` để không track `bin/`, `obj/`

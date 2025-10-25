-- TÙY CHỌN: chữ ký thử nghiệm VÔ HẠI cho phương pháp HASH TRUYỀN THỐNG.
-- Tạo mẫu ASCII (không BOM): SIM-MALWARE-SIGNATURE::A57C1F37::PAYLOAD
-- Chỉ khớp CHÍNH XÁC tệp mẫu trên; không phát hiện vì tên tệp hoặc chuỗi tiền tố.
-- Mặc định IsActive=0; chỉ bật nếu kiểm thử trong môi trường cô lập.
USE AntivirusDB;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.VirusSignatures WHERE MalwareName=N'Demo.Hash.SafeFixture')
    INSERT dbo.VirusSignatures
        (MalwareName, MalwareFamily, Category, SignatureType, SHA256, Severity, Description, IsActive)
    VALUES
        (N'Demo.Hash.SafeFixture', N'Test', N'Test', 'Hash',
         'ea9958fec77e564d36bd910ca32d15004e41fdb21bfb7c1dca414da943ee3eeb',
         'Low', N'Mẫu ASCII vô hại; chỉ dùng kiểm thử hash toàn tệp.', 0);
GO
-- Khi kiểm thử có chủ đích mới bật:
-- UPDATE dbo.VirusSignatures SET IsActive=1 WHERE MalwareName=N'Demo.Hash.SafeFixture';

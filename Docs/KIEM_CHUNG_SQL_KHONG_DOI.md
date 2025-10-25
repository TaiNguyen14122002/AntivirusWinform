# Đối chiếu cấu trúc và dữ liệu SQL gốc

Bản nâng cấp không thay đổi các tệp SQL, migration hoặc backup so với ZIP đầu vào. SHA-256 dưới đây đối chiếu theo byte, không phải kết quả chạy thử SQL Server.

| Tệp | Kết quả | SHA-256 hiện tại |
|---|---|---|
| `AntivirusDB.ChunkHash.DemoData.sql` | KHÔNG ĐỔI | `21eda8d95d4563313495e1f1a44f4b8de063c43962a57a9d8debbfc338b3abe4` |
| `AntivirusDB.ChunkHash.Migration.sql` | KHÔNG ĐỔI | `65e141336adbc7539ba416864707fdef58b20afb1ffb9430fbcccf7da59062c8` |
| `AntivirusDB.Hash.DemoData.sql` | KHÔNG ĐỔI | `6898522079c89fca26ce1de4b1ecdec0461e23af52d85c339e68f7a06bf88e87` |
| `AntivirusDB.bak` | KHÔNG ĐỔI | `ed46b8f55316f52885c6eeba40551432756668896a9c3c0b011bc5e44d9fedf8` |
| `AntivirusDB.extensions.sql` | KHÔNG ĐỔI | `f4f736a2e24e99123ebfce7395f35b4059e1fc6b7cd9a9b8b523c7f6b04226b3` |
| `AntivirusDB.schema.sql` | KHÔNG ĐỔI | `bb745b0cf519b44266c1771900265cd287d3050f776da1be65f1aedc727886c3` |
| `AntivirusDB.sql` | KHÔNG ĐỔI | `19088a29ae85e363a754542b9638240ea90354f85e38cabbd04f11fe3515b5a9` |
| `BAT_MAU_HASH_THU_NGHIEM_AN_TOAN.sql` | KHÔNG ĐỔI | `c065dbad2bb0a151131bc1f9271e7a5ac8f486c327b3982a17a4905f5e6912d6` |
| `KIEM_TRA_CHU_KY_NHAP_TU_CSV.sql` | KHÔNG ĐỔI | `7224f2db47b2d2b8aa37304752b6d64d62bb9ae2d447f117cc4bb5c2b591fe40` |
| `KIEM_TRA_FULL_EXPORT.sql` | KHÔNG ĐỔI | `f78f216d1fc544887cfd48deccd4178aa3469d79ad7833d62a486deea0c1bbe0` |
| `KIEM_TRA_KHONG_PHAT_HIEN.sql` | KHÔNG ĐỔI | `0698bdccd33660fd9718060eed5aeb3a2d6d3ae0ab35b4f16a2772cfcdfa223d` |
| `KIEM_TRA_MALWAREBAZAAR_SQL.sql` | KHÔNG ĐỔI | `5695600988f2ce1994819e084ece9ef75e4a9bdcf07cda8f7734220f37e93d69` |
| `Migrations/001_QuanLyTrangThaiChuKy.sql` | KHÔNG ĐỔI | `773daaa29e6421dbfbd94cdca177805abb9fed1764b1b4a30307fd8c326b7a8c` |
| `Migrations/002_ScanFileIssues.sql` | KHÔNG ĐỔI | `7ff0aba0cfb899803e35c426fb14782dacdac95541c53c49b4e90cff5fe1b175` |
| `Migrations/003_ScanSignatureMetadata.sql` | KHÔNG ĐỔI | `1379d2eb97e70d2c8708276ea5c5bda195dec96dac0846599b77552bc44a96b0` |
| `Migrations/004_QuanLyChuKy_LichQuet_NgoaiLe.sql` | KHÔNG ĐỔI | `7b2e16f082014c74711b6df80066376d59bffce66ec9e06c05c59eb1431fec4a` |

**Lưu ý:** Các migration lịch sử được giữ nguyên để không mất nguồn dự án nhưng không được tự động áp dụng bởi đợt nâng cấp này.

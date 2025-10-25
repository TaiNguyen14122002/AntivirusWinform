# Hai phương pháp hàm băm đang dùng trong AntivirusWinform

Từ bản cập nhật này, **chỉ còn**:

1. **Hash truyền thống**: SHA-256, SHA-1, MD5 toàn tệp, tra cứu `VirusSignatures`.
2. **Chunk Hash + Sliding Window + Rolling Hash (CDC)**: cửa sổ 48 byte trượt qua luồng dữ liệu; rolling hash xác định trigger point; cắt thành chunk động và tính SHA-256 mỗi chunk; so khớp nhiều chunk đúng thứ tự và ngưỡng tương đồng với `ChunkSignatureGroups` + `ChunkSignatures`.

**Đã bỏ** nhánh quét fixed-window chunk độc lập. Các nhóm cũ có `ChunkingMode = 'FixedWindow'` không còn được engine dùng để phát hiện, nhưng dữ liệu SQL được giữ nguyên để sao lưu/chuyển đổi có kiểm chứng.

Xem `HUONG_DAN_CDC_FUZZY_HASHING.md` và `HUONG_DAN_2_PHUONG_PHAP_HASH.md`.

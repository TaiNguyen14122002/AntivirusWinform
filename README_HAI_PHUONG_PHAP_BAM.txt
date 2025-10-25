ANTIVIRUSWINFORM - 16/10/2026

CHI CON 2 PHUONG PHAP BAM TRONG ENGINE QUET:
1. HASH TRUYEN THONG: SHA-256 / SHA-1 / MD5 toan tep; VirusSignatures.
2. CHUNK HASH + SLIDING WINDOW + ROLLING HASH: CDC content-defined chunks;
   tim trigger rolling hash, SHA-256 cho tung chunk; kiem tra thu tu/nguong
   ChunkSignatureGroups + ChunkSignatures voi ChunkingMode='CDC'.

DA NGUNG: fixed window chunk doc lap; detect tu ten EICAR, byte-prefix,
heuristic. Khong xoa du lieu SQL cu, nhung bo qua nhom FixedWindow.

CSDL da co: backup, chay AntivirusDB.ChunkHash.Migration.sql (neu can).
Moi ky CDC: tao bang Tests\CdcSignatureBuilder.exe tu mau da xac minh.
KHONG gan ChunkingMode='CDC' cho nhom fixed cu neu chua tai tao byte goc.

Chay VERIFY_HYBRID_STREAMING.cmd va VERIFY_CDC_WINDOWS.cmd de tu kiem tra.
Phai build va test Windows/.NET Framework 4.7.2 va SQL Server thuc te.
Chi tiet: HUONG_DAN_2_PHUONG_PHAP_HASH.md

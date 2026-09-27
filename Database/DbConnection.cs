
using System;
using System.Data;
using System.Data.SqlClient;

namespace ScanAndRemoveVirus.Database
{
    public static class DbConnection
    {
        // =====================================
        // CẤU HÌNH SQL SERVER
        // =====================================

        private const string Server =
            @"localhost\SQLEXPRESS";

        private const string Database =
            "AntivirusDB";

        private static readonly string ConnectionString =
            $@"Server={Server};
               Database={Database};
               Integrated Security=True;
               Connect Timeout=10;
               MultipleActiveResultSets=True;";

        // =====================================
        // TẠO KẾT NỐI
        // =====================================

        public static SqlConnection Create()
        {
            return new SqlConnection(ConnectionString);
        }

        // =====================================
        // KIỂM TRA KẾT NỐI
        // =====================================

        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = Create())
                {
                    conn.Open();

                    return conn.State ==
                           ConnectionState.Open;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================
        // LẤY TRẠNG THÁI KẾT NỐI
        // =====================================

        public static string GetConnectionStatus()
        {
            try
            {
                using (SqlConnection conn = Create())
                {
                    conn.Open();

                    return
                        "Kết nối SQL Server thành công!"
                        + Environment.NewLine
                        + Environment.NewLine
                        + "Server: " + conn.DataSource
                        + Environment.NewLine
                        + "Database: " + conn.Database;
                }
            }
            catch (Exception ex)
            {
                return
                    "Kết nối SQL Server thất bại!"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Chi tiết lỗi: " + ex.Message;
            }
        }

        // Giữ tương thích với tên phương thức cũ
        public static string GetConnectionString()
        {
            return GetConnectionStatus();
        }
    }
}

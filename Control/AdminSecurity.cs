using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ScanAndRemoveVirus.Control
{
    /// <summary>
    /// Authentication for the local administrator workspace. Only a salted PBKDF2
    /// verifier is compiled; user-specific replacements are DPAPI protected.
    /// This is app-level access control, not an OS security boundary.
    /// </summary>
    internal static class AdminSecurity
    {
        internal const string Username = "TaiNguyen";
        private const int Iterations = 180000;
        private const int MaxFailures = 5;
        private static readonly byte[] InitialSalt = ConvertHex("2a6f4985d6d5964fc36ce9270ee1e6ff");
        private static readonly byte[] InitialVerifier = ConvertHex("86bc72a903e73a33f5bb1bf91b10365061b2d13d5d1bb6f37524f2645eb9c42f");
        private static int failures;
        private static DateTime lockedUntil;
        // Session quản trị chỉ tồn tại trong bộ nhớ của tiến trình (không lưu mật khẩu/token ra ổ đĩa).
        // Phiên duy trì khi chuyển trang, nhưng kết thúc sau 30 phút không thao tác,
        // tối đa 8 giờ kể từ lần đăng nhập và luôn kết thúc khi đăng xuất/thoát ứng dụng.
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromHours(8);
        private static readonly object SessionGate = new object();
        private static AdminSession currentSession;
        private static AdminActivityFilter activityFilter;

        private sealed class AdminSession
        {
            internal readonly string Id;
            internal readonly DateTime CreatedUtc;
            internal DateTime LastActivityUtc;
            internal AdminSession(DateTime now)
            {
                byte[] random = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(random);
                Id = Convert.ToBase64String(random);
                Array.Clear(random, 0, random.Length);
                CreatedUtc = now;
                LastActivityUtc = now;
            }
        }

        private sealed class AdminActivityFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message message)
            {
                // Chỉ hành động thực của người dùng mới gia hạn phiên;
                // không dùng IsAuthenticated/Timer vì quá trình vẽ lại hay tải SQL
                // có thể tự gia hạn phiên dù máy không được sử dụng.
                int code = message.Msg;
                if ((code >= 0x0200 && code <= 0x020E) || // chuột
                    (code >= 0x0100 && code <= 0x0109))   // bàn phím
                    RecordUserActivity();
                return false;
            }
        }
        private static string CredentialFile
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ScanAndRemoveVirus", "admin-auth.dpapi"); }
        }
        private static byte[] ConvertHex(string value)
        {
            byte[] bytes = new byte[value.Length / 2];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            return bytes;
        }
        internal static bool IsAuthenticated
        {
            get
            {
                lock (SessionGate)
                {
                    if (currentSession == null) return false;
                    DateTime now = DateTime.UtcNow;
                    if (now - currentSession.LastActivityUtc >= IdleTimeout ||
                        now - currentSession.CreatedUtc >= AbsoluteTimeout ||
                        now < currentSession.CreatedUtc)
                    {
                        currentSession = null;
                        return false;
                    }
                    return true;
                }
            }
        }

        internal static void Lock()
        {
            // Đăng xuất tường minh; không gọi khi người dùng chỉ chuyển trang.
            lock (SessionGate) currentSession = null;
        }

        internal static void Shutdown()
        {
            Lock();
            // Gỡ bộ theo dõi thao tác khi ứng dụng kết thúc.
            if (activityFilter != null)
            {
                Application.RemoveMessageFilter(activityFilter);
                activityFilter = null;
            }
        }

        private static void RecordUserActivity()
        {
            lock (SessionGate)
            {
                if (currentSession == null) return;
                DateTime now = DateTime.UtcNow;
                if (now - currentSession.LastActivityUtc >= IdleTimeout ||
                    now - currentSession.CreatedUtc >= AbsoluteTimeout ||
                    now < currentSession.CreatedUtc)
                {
                    currentSession = null;
                    return;
                }
                currentSession.LastActivityUtc = now;
            }
        }

        private static void StartSession()
        {
            lock (SessionGate) currentSession = new AdminSession(DateTime.UtcNow);
            if (activityFilter == null)
            {
                activityFilter = new AdminActivityFilter();
                // Login() được gọi từ UI thread của hộp thoại WinForms.
                Application.AddMessageFilter(activityFilter);
            }
        }
        internal static bool EnsureLogin(IWin32Window owner)
        {
            if (IsAuthenticated) return true;
            using (var dialog = new AdminLoginForm())
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK) return false;
                return IsAuthenticated;
            }
        }
        internal static bool Login(string username, string password, out string error)
        {
            error = "Tên đăng nhập hoặc mật khẩu không đúng.";
            if (DateTime.UtcNow < lockedUntil)
            {
                error = "Đã nhập sai quá nhiều lần. Hãy thử lại sau " +
                    Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalMinutes) + " phút.";
                return false;
            }
            byte[] salt; byte[] verifier;
            ReadVerifier(out salt, out verifier);
            byte[] actual;
            using (var kdf = new Rfc2898DeriveBytes(password ?? "", salt, Iterations)) actual = kdf.GetBytes(32);
            int difference = 0;
            for (int i = 0; i < verifier.Length; i++) difference |= verifier[i] ^ actual[i];
            Array.Clear(actual, 0, actual.Length);
            bool correct = string.Equals(username, Username, StringComparison.Ordinal) && difference == 0;
            if (!correct)
            {
                failures++;
                if (failures >= MaxFailures)
                {
                    lockedUntil = DateTime.UtcNow.AddMinutes(15);
                    failures = 0;
                }
                AdminAudit.Write("login-failure", "Đăng nhập quản trị thất bại");
                return false;
            }
            failures = 0; lockedUntil = DateTime.MinValue;
            StartSession();
            AdminAudit.Write("login-success", "Xác thực quản trị thành công");
            return true;
        }
        internal static bool ChangePassword(string nextPassword, out string error)
        {
            error = null;
            if (!IsAuthenticated) { error = "Cần đăng nhập quản trị."; return false; }
            if (string.IsNullOrWhiteSpace(nextPassword) || nextPassword.Length < 12)
            { error = "Mật khẩu mới phải có ít nhất 12 ký tự."; return false; }
            byte[] salt = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            byte[] key;
            using (var kdf = new Rfc2898DeriveBytes(nextPassword, salt, Iterations)) key = kdf.GetBytes(32);
            try
            {
                string file = CredentialFile;
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                byte[] payload = new byte[48];
                Buffer.BlockCopy(salt, 0, payload, 0, 16);
                Buffer.BlockCopy(key, 0, payload, 16, 32);
                File.WriteAllBytes(file, ProtectedData.Protect(payload, null, DataProtectionScope.CurrentUser));
                Array.Clear(payload, 0, payload.Length);
                AdminAudit.Write("password-change", "Đã đổi mật khẩu quản trị cho tài khoản Windows hiện tại");
                return true;
            }
            catch (Exception ex) { error = "Không lưu được mật khẩu mới: " + ex.Message; return false; }
            finally { Array.Clear(key, 0, key.Length); }
        }
        private static void ReadVerifier(out byte[] salt, out byte[] verifier)
        {
            salt = InitialSalt; verifier = InitialVerifier;
            try
            {
                if (!File.Exists(CredentialFile)) return;
                byte[] contents = ProtectedData.Unprotect(File.ReadAllBytes(CredentialFile), null,
                    DataProtectionScope.CurrentUser);
                if (contents.Length != 48) throw new CryptographicException("Invalid verifier");
                salt = new byte[16]; verifier = new byte[32];
                Buffer.BlockCopy(contents, 0, salt, 0, 16);
                Buffer.BlockCopy(contents, 16, verifier, 0, 32);
                Array.Clear(contents, 0, contents.Length);
            }
            catch (Exception)
            {
                // Fail closed: a damaged configured verifier must NOT fall back to the default password.
                if (File.Exists(CredentialFile)) { salt = new byte[16]; verifier = new byte[32]; }
            }
        }
    }

    internal sealed class AdminLoginForm : Form
    {
        internal AdminLoginForm()
        {
            Text = "Đăng nhập quản trị Antivirus";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            ClientSize = new Size(420, 260); BackColor = Color.FromArgb(246, 249, 253);
            Font = new Font(Theme.UiFontFamily, 10.65f);
            var title = new Label { Text = "Xác thực quản trị viên", Font = new Font(Theme.UiFontFamily, 17.25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 58, 103), Bounds = new Rectangle(24, 17, 366, 38) };
            var userLabel = new Label { Text = "Tên đăng nhập", Bounds = new Rectangle(24, 64, 180, 22) };
            var user = new TextBox { Bounds = new Rectangle(24, 87, 370, 28), Text = AdminSecurity.Username };
            var passLabel = new Label { Text = "Mật khẩu", Bounds = new Rectangle(24, 124, 180, 22) };
            var pass = new TextBox { Bounds = new Rectangle(24, 148, 370, 28), UseSystemPasswordChar = true };
            var warning = new Label { Bounds = new Rectangle(24, 184, 365, 28), ForeColor = Color.Firebrick };
            var login = new Button { Text = "Đăng nhập", Bounds = new Rectangle(265, 217, 128, 32),
                BackColor = Color.FromArgb(34, 103, 217), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "Hủy", Bounds = new Rectangle(154, 217, 102, 32), DialogResult = DialogResult.Cancel };
            login.Click += (s, e) =>
            {
                string message;
                if (AdminSecurity.Login(user.Text, pass.Text, out message)) { DialogResult = DialogResult.OK; Close(); }
                else { warning.Text = message; pass.Clear(); pass.Focus(); }
            };
            Controls.AddRange(new System.Windows.Forms.Control[] { title, userLabel, user, passLabel, pass, warning, login, cancel });
            AcceptButton = login; CancelButton = cancel;
            Shown += (s, e) => pass.Focus();
        }
    }
}

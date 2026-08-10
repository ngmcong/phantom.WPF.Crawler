using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace Crawler
{
    internal class SqliteHelper
    {
        private readonly string _connectionString;

        // Khoản khởi tạo: nhận vào tên/đường dẫn file db (ví dụ: "data.db")
        public SqliteHelper(string dbPath = "urls.db")
        {
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        // Tự động tạo bảng nếu chưa tồn tại
        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string createTableSql = @"
                CREATE TABLE IF NOT EXISTS Urls (
                    Url TEXT PRIMARY KEY
                );"; // Dùng PRIMARY KEY nếu bạn muốn tự động chặn trùng lặp URL

                using (var command = new SqliteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // Hàm chèn 1 dòng URL (Thực thi Async để không làm treo UI WPF)
        public async Task<bool> InsertUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Dùng INSERT OR IGNORE để nếu chèn URL đã tồn tại thì sẽ bỏ qua mà không quăng Exception
                string insertSql = "INSERT OR IGNORE INTO Urls (Url) VALUES (@url);";

                using (var command = new SqliteCommand(insertSql, connection))
                {
                    // Dùng Parameterized Query để tránh lỗi SQL Injection và lỗi ký tự đặc biệt
                    command.Parameters.AddWithValue("@url", url);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0; // Trả về true nếu chèn thành công dòng mới
                }
            }
        }

        public async Task<int> InsertUrlsBatchAsync(IEnumerable<string> urls)
        {
            if (urls == null || !urls.Any()) return 0;

            int insertedCount = 0;

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 🔑 TẠO TRANSACTION: Tất cả câu lệnh INSERT bên trong sẽ được gom lại ghi 1 lần
                using (var transaction = connection.BeginTransaction())
                {
                    string insertSql = "INSERT OR IGNORE INTO Urls (Url) VALUES (@url);";

                    using (var command = new SqliteCommand(insertSql, connection, transaction))
                    {
                        // Khai báo parameter trước vòng lặp để tối ưu bộ nhớ
                        var urlParam = command.Parameters.Add("@url", SqliteType.Text);
                        var totalCount = urls.Count();
                        foreach (var url in urls)
                        {
                            if (string.IsNullOrWhiteSpace(url)) continue;

                            // Gán giá trị mới cho Parameter
                            urlParam.Value = url;

                            // Thực thi câu lệnh
                            int rows = await command.ExecuteNonQueryAsync();
                            if (rows > 0) insertedCount++;
                            Debug.WriteLine($"Inserted {insertedCount} per {totalCount}");
                        }
                    }

                    // 🔑 XÁC NHẬN LƯU TẤT CẢ DỮ LIỆU XUỐNG ĐĨA CÙNG MỘT LÚC
                    await transaction.CommitAsync();
                }
            }

            return insertedCount; // Trả về số lượng URL mới thực sự được thêm vào DB
        }

        public async Task<IEnumerable<string>> GetAllUrlsAsync()
        {
            var urlList = new List<string>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string selectSql = "SELECT Url FROM Urls;";

                using (var command = new SqliteCommand(selectSql, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Đọc cột đầu tiên (index 0) là Url
                        string url = reader.GetString(0);
                        urlList.Add(url);
                    }
                }
            }

            return urlList;
        }
    }
}
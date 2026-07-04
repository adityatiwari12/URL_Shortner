using Microsoft.Data.SqlClient;
using UrlShortener.Models;

namespace UrlShortener.Data
{
    /// <summary>
    /// All raw ADO.NET database access lives here. No Entity Framework is used anywhere.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing from appsettings.json.");
        }

        /// <summary>
        /// Checks whether a shortcode is already taken.
        /// </summary>
        public bool ShortCodeExists(string shortCode)
        {
            const string sql = "SELECT COUNT(1) FROM Urls WHERE ShortCode = @ShortCode";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShortCode", shortCode);

            connection.Open();
            var count = (int)command.ExecuteScalar();
            return count > 0;
        }

        /// <summary>
        /// Inserts a new shortened URL record and returns the generated row, including
        /// the identity Id and default CreatedAt/ClickCount values assigned by SQL Server.
        /// </summary>
        public UrlModel InsertUrl(string originalUrl, string shortCode)
        {
            const string sql = @"
                INSERT INTO Urls (OriginalUrl, ShortCode)
                OUTPUT INSERTED.Id, INSERTED.OriginalUrl, INSERTED.ShortCode, INSERTED.CreatedAt, INSERTED.ClickCount
                VALUES (@OriginalUrl, @ShortCode);";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@OriginalUrl", originalUrl);
            command.Parameters.AddWithValue("@ShortCode", shortCode);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return MapReaderToUrlModel(reader);
            }

            throw new InvalidOperationException("Insert did not return the created row.");
        }

        /// <summary>
        /// Returns every shortened URL, newest first.
        /// </summary>
        public List<UrlModel> GetAllUrls()
        {
            const string sql = @"
                SELECT Id, OriginalUrl, ShortCode, CreatedAt, ClickCount
                FROM Urls
                ORDER BY CreatedAt DESC;";

            var urls = new List<UrlModel>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);

            connection.Open();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                urls.Add(MapReaderToUrlModel(reader));
            }

            return urls;
        }

        /// <summary>
        /// Looks up a URL by its shortcode. Returns null if not found.
        /// </summary>
        public UrlModel? GetByShortCode(string shortCode)
        {
            const string sql = @"
                SELECT Id, OriginalUrl, ShortCode, CreatedAt, ClickCount
                FROM Urls
                WHERE ShortCode = @ShortCode;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShortCode", shortCode);

            connection.Open();
            using var reader = command.ExecuteReader();

            return reader.Read() ? MapReaderToUrlModel(reader) : null;
        }

        /// <summary>
        /// Atomically increments the click count for a shortcode.
        /// </summary>
        public void IncrementClickCount(string shortCode)
        {
            const string sql = @"
                UPDATE Urls
                SET ClickCount = ClickCount + 1
                WHERE ShortCode = @ShortCode;";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShortCode", shortCode);

            connection.Open();
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes a URL by shortcode. Returns true if a row was removed.
        /// </summary>
        public bool DeleteByShortCode(string shortCode)
        {
            const string sql = "DELETE FROM Urls WHERE ShortCode = @ShortCode";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShortCode", shortCode);

            connection.Open();
            var rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        private static UrlModel MapReaderToUrlModel(SqlDataReader reader)
        {
            return new UrlModel
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                OriginalUrl = reader.GetString(reader.GetOrdinal("OriginalUrl")),
                ShortCode = reader.GetString(reader.GetOrdinal("ShortCode")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                ClickCount = reader.GetInt32(reader.GetOrdinal("ClickCount"))
            };
        }
    }
}

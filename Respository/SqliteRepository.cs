using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqliteDataReader.Respository
{
    public class SqliteRepository
    {
        private readonly string _connectionString;

        public SqliteRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqliteRepository(FileInfo file)
        {
            _connectionString = $"Data Source={file.FullName}";
        }

        public async Task<T[]> Query<T>(string sql, object? param = null)
        {
            try
            {
                T[] results;
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    var asyncResults = await conn.QueryAsync<T>(sql, param);
                    results = asyncResults.ToArray();
                }
                return results;
            }
            catch (Exception e)
            {
                e.ShowMessage();
                return new T[0];
            }
        }

        public async Task<DataTable?> QueryReader(string sql, object? param = null)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    var reader = await conn.ExecuteReaderAsync(sql, param);
                    return await reader.ToDataTable();
                    //var dict = reader.ExtractReader();
                    //return dict.ToDataTable();
                    //return reader.GetSchemaTable();
                }
            }
            catch (Exception e)
            {
                e.ShowMessage();
                return null;
            }
        }

        public async Task<string[]> GetTables() =>
            await Query<string>("SELECT name FROM sqlite_master WHERE type='table'");
    }
}

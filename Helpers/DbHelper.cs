using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace K3CloudDataDictionary.Helpers
{
    public static class DbHelper
    {
        public static bool TestConnection(string connectionString, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static List<Dictionary<string, object>> ExecuteQuery(string connectionString, string sql)
        {
            var results = new List<Dictionary<string, object>>();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 60;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string colName = reader.GetName(i);
                                row[colName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            }

            return results;
        }

        public static object ExecuteScalar(string connectionString, string sql)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 60;
                    return cmd.ExecuteScalar();
                }
            }
        }
    }
}

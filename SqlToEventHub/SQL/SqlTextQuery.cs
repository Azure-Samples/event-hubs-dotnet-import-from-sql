using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace WorkerHost.SQL
{
    public class SqlTextQuery
    {
        private readonly string _sqlDatabaseConnectionString;

        public SqlTextQuery(string sqlDatabaseConnectionString)
        {
            _sqlDatabaseConnectionString = sqlDatabaseConnectionString;
        }

        public bool RunSqlCommand(string query)
        {
            var command = PrepareSqlCommand(query);

            return PerformNonQuery(command);
        }
        public IEnumerable<Dictionary<string, object>> PerformQuery(string query)
        {
            var command = PrepareSqlCommand(query);
            return PerformQuery(command);
        }

        private IEnumerable<Dictionary<string, object>> PerformQuery(SqlCommand commandToRun)
        {
            IEnumerable<Dictionary<string, object>> result = null;
            try
            {
                using (var sqlConnection = new SqlConnection(_sqlDatabaseConnectionString))
                {
                    sqlConnection.Open();

                    commandToRun.Connection = sqlConnection;
                    using (SqlDataReader r = commandToRun.ExecuteReader())
                    {
                        result = Serialize(r);
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        private bool PerformNonQuery(SqlCommand commandToRun)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(_sqlDatabaseConnectionString))
                {
                    sqlConnection.Open();

                    commandToRun.Connection = sqlConnection;
                    commandToRun.ExecuteNonQuery();

                    sqlConnection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        private SqlCommand PrepareSqlCommand(string query)
        {
            SqlCommand command = new SqlCommand(query)
            {
                CommandType = CommandType.Text
            };
            return command;
        }

        private IEnumerable<Dictionary<string, object>> Serialize(SqlDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row.Add(reader.GetName(i), reader.GetValue(i));
                }
                results.Add(row);
            }
            return results;
        }
    }
}

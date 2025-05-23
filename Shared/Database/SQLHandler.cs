using Microsoft.Data.Sqlite;
using System.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tShockDiscordVerifier.Shared.Database
{
    class SQLHandler : IDisposable
    {
		//public static SQLHandler Instance => Core.DBHandler;

		private IDbConnection connection;
		public SQLHandler()
		{
			string dbPath = Path.Combine(TShockAPI.TShock.SavePath, Resources.DatabaseLocation);
			connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            ExecuteCommand(
                @$"
                CREATE TABLE IF NOT EXISTS Accounts (
				{Resources.ColPrimaryKey} INTEGER PRIMARY KEY AUTOINCREMENT,
				{Resources.ColUsername} TEXT UNIQUE NOT NULL,
				{Resources.ColDiscordID} UNSIGNED BIG INT DEFAULT 0
				);"
            );
		}

		public bool TryGetUsersFromID(ulong discID, out IEnumerable<string?>? accountNames)
		{
            accountNames = ExecuteVector<string>(
                $@"SELECT {Resources.ColUsername} FROM Accounts WHERE {Resources.ColDiscordID} IS ({discID});",
                Resources.ColUsername);
            return accountNames is not null && accountNames?.Count() > 0;
		}
		public bool TryGetIDFromUsername(string username, out ulong? discordID)
		{
            discordID = ExecuteScalar<ulong?>(
                $@"SELECT {Resources.ColDiscordID} FROM Accounts WHERE {Resources.ColUsername} IS ({username});",
                Resources.ColDiscordID
                );
            return discordID is not null;
		}

		public void Dispose()
		{
            connection.Close();
			connection?.Dispose();
		}

        internal IDbCommand CreateCommand() => connection.CreateCommand();
        internal IDbCommand CreateCommand(string text)
        {
            var result = connection.CreateCommand();
            if (!text.EndsWith(";")) //I kept getting lazy with writing them
                text += ";";
            result.CommandText = text;
            return result;
        }

        internal int ExecuteCommand(string text) => CreateCommand(text).ExecuteNonQuery();
        internal IReadOnlyList<IReadOnlyDictionary<string, object?>> ExecuteQuery(string text)
        {
            IDbCommand cmd = CreateCommand(text);
            List<Dictionary<string, object?>> resultList = new List<Dictionary<string, object?>>();

            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Dictionary<string, object?> row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    resultList.Add(row);
                }
            }
            return resultList;
        }
        internal bool TryExecuteQuery(string text, out IReadOnlyList<IReadOnlyDictionary<string, object?>> results)
        {
            results = ExecuteQuery(text);
            return results.Count != 0;
        }

        internal IReadOnlyDictionary<string, object?>? ExecuteFirst(string text)
        {
            if (TryExecuteQuery(text, out var results))
            {
                return results[0];
            }
            return null;
        }
        internal bool TryExecuteFirst(string text, out IReadOnlyDictionary<string, object?>? results)
        {
            results = ExecuteFirst(text);
            return results is not null && results.Count != 0;
        }

        internal T? ExecuteScalar<T>(string text, string column)
        {
            if (TryExecuteFirst(text, out var results))
            {
                return (T?)results![column];
            }
            return default(T?);
        }
        internal bool TryExecuteScalar<T>(string text, string column, out T? result)
        {
            result = ExecuteScalar<T>(text, column);
            return result is not null;
        }

        internal IEnumerable<T>? ExecuteVector<T>(string text, string column)
        {
            if (TryExecuteQuery(text, out var results))
            {
                return results.Where(w => w.ContainsKey(column) && w[column] is not null).Select(s => (T)s[column]!);
            }
            return null;
        }
        internal bool TryExecuteVector<T>(string text, string column, out IEnumerable<T>? result)
        {
            result = ExecuteVector<T>(text, column);
            return result is not null && result.Count() != 0;
        }
    }
}
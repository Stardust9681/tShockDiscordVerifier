using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static SqlKata.Extensions.QueryForExtensions;
using static SqlKata.Execution.QueryExtensions;

namespace tShockDiscordVerifier.Shared.Database
{
    class SQLHandler : IDisposable
    {
		public static SQLHandler Instance => Core.DBHandler;

		private System.Data.IDbConnection connection;
		private SqlKata.Compilers.Compiler sqlCompiler;
		public SqlKata.Execution.QueryFactory DB { get; private set; }
		public SQLHandler()
		{
			string dbPath = Path.Combine(TShockAPI.TShock.SavePath, Resources.DatabaseLocation);
			connection = new SqliteConnection($"Data Source={dbPath}");
			sqlCompiler = new SqlKata.Compilers.SqliteCompiler();
			DB = new SqlKata.Execution.QueryFactory(connection, sqlCompiler);
			DB.Statement(
				@"
				CREATE TABLE IF NOT EXISTS Accounts (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				Username TEXT UNIQUE NOT NULL,
				DiscordID UNSIGNED BIG INT DEFAULT 0
				);"
			);
		}

		public bool TryGetUsersFromID(ulong discID, out IEnumerable<string>? accountNames)
		{
			accountNames = (IEnumerable<string>)DB.Query("Accounts")
				.Select(Resources.ColUsername)
				.Where(Resources.ColDiscordID, discID)
				.Get();
			if (accountNames is null || !accountNames.Any()) return false;
			return true;
		}
		public bool TryGetIDFromUsername(string username, out ulong discordID)
		{
			dynamic attemptedID = DB.Query("Accounts")
				.Select(Resources.ColDiscordID)
				.Where(Resources.ColUsername, username)
				.First();
			if (attemptedID is null)
			{
				discordID = 0;
				return false;
			}
			discordID = (ulong)attemptedID;
			return true;
		}

		public void Dispose()
		{
			DB.Dispose();
			connection?.Dispose();
		}
    }
}

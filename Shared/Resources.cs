using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tShockDiscordVerifier.Shared
{
    public class Resources
    {
		#region Config
		public const string ConfigFolderName = "DiscordVerifier";
		public static readonly string ConfigDirectory = Path.Combine(TShockAPI.TShock.SavePath, ConfigFolderName);

		public const string DatabaseConfigFileName = "database_config.json";
		public static readonly string DatabaseConfigPath = Path.Combine(ConfigDirectory, DatabaseConfigFileName);
		public const string DiscordConfigFileName = "discord_config.json";
		public static readonly string DiscordConfigPath = Path.Combine(ConfigDirectory, DiscordConfigFileName);
		public const string TShockConfigFileName = "tshock_config.json";
		public static readonly string TShockConfigPath = Path.Combine(ConfigDirectory, TShockConfigFileName);
		#endregion

		#region Database
		public const string ColUsername = "Username";
		public const string ColDiscordID = "DiscordID";
		public const string ColPrimaryKey = "Id";
		public const string DatabaseLocation = "auth_entries.sqlite3";
		#endregion
	}
}

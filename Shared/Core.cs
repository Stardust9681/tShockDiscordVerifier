using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;

namespace tShockDiscordVerifier.Shared
{
    class Core
    {
		#region Config
		public static bool ConfigInitialised { get; private set; } = false;

		private static DiscordBot.BotConfigFile BotConfigFile;
		public static DiscordBot.BotConfig? BotConfig => BotConfigFile.Settings;

		private static TShockPlugin.PluginConfigFile PluginConfigFile;
		public static TShockPlugin.PluginConfig PluginConfig => PluginConfigFile.Settings;

		internal static void InitConfig()
		{
			BotConfigFile = new DiscordBot.BotConfigFile();
			if (!BotConfigFile.TryRead(out _))
			{
				Directory.CreateDirectory(DiscordBot.BotConfigFile.Directory);
				BotConfigFile.Settings = new DiscordBot.BotConfig();
				TShockAPI.TShock.Log.ConsoleInfo($"Discord Config File not found. Generating at {Resources.DiscordConfigPath}");
				BotConfigFile.Write(DiscordBot.BotConfigFile.Path);
				BotConfigFile.Read(DiscordBot.BotConfigFile.Path, out _);
			}

			PluginConfigFile = new TShockPlugin.PluginConfigFile();
			if (!PluginConfigFile.TryRead(out _))
			{
				Directory.CreateDirectory(TShockPlugin.PluginConfigFile.Directory);
				PluginConfigFile.Settings = new TShockPlugin.PluginConfig();
				TShockAPI.TShock.Log.ConsoleInfo($"Plugin Config File not found. Generating at {Resources.TShockConfigPath}");
				PluginConfigFile.Write(TShockPlugin.PluginConfigFile.Path);
				PluginConfigFile.Read(TShockPlugin.PluginConfigFile.Path, out _);
			}

			ConfigInitialised = true;
		}
		#endregion

		#region Database
		public static bool DatabaseInitialised { get; private set; } = false;
		public static Database.SQLHandler DBHandler { get; private set; }
		public static SqlKata.Execution.QueryFactory QueryBuilder => DBHandler.DB;

		internal static void InitDB()
		{
			DBHandler = new Database.SQLHandler();

			DatabaseInitialised = true;
		}
		#endregion

		#region Discord Bot
		public static DiscordBot.BotClient Client { get; private set; }
		public static bool BotInitialised => Client.Ready;
		internal static void InitDiscordBot()
		{
			if (string.IsNullOrEmpty(BotConfig.Token))
			{
				TShockAPI.TShock.Log.ConsoleError("== NO BOT TOKEN PROVIDED (tShockDiscordVerifier) ==");
				TShockAPI.TShock.Log.ConsoleWarn("== NO BOT TOKEN PROVIDED (tShockDiscordVerifier) ==");
				TShockAPI.TShock.Log.ConsoleInfo("== NO BOT TOKEN PROVIDED (tShockDiscordVerifier) ==");
				TShockAPI.TShock.Log.ConsoleError($"=== SET TOKEN AT {Resources.DiscordConfigPath} ===");
				TShockAPI.TShock.Log.ConsoleWarn($"=== SET TOKEN AT {Resources.DiscordConfigPath} ===");
				TShockAPI.TShock.Log.ConsoleInfo($"=== SET TOKEN AT {Resources.DiscordConfigPath} ===");
				return;
			}
			Client = new DiscordBot.BotClient();
		}
		#endregion

		#region TShock Plugin
		public static TShockPlugin.TShockManager PluginHandler { get; private set; }
		public static bool PluginInitialised { get; private set; }
		internal static void InitPlugin(TerrariaPlugin plugin)
		{
			PluginHandler = new TShockPlugin.TShockManager(plugin);
			PluginInitialised = true;
		}
		#endregion

		internal static void InitAll(TerrariaPlugin plugin)
		{
			InitConfig();
			InitDB();
			InitDiscordBot();
			InitPlugin(plugin);
		}
		internal static void DisposeAll()
		{
			DBHandler.Dispose();
			Client.Dispose();
			PluginHandler.Dispose();

			Core.DatabaseInitialised = false;
			Core.PluginInitialised = false;
			Core.ConfigInitialised = false;
		}
    }
}

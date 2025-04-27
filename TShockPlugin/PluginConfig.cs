using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using TShockAPI.Configuration;
using tShockDiscordVerifier.Shared;

namespace tShockDiscordVerifier.TShockPlugin
{
    public class PluginConfigFile : ConfigFile<PluginConfig>
    {
		public const string Folder = Resources.ConfigFolderName;
		public const string File = Resources.TShockConfigFileName;
		public static string Directory => Resources.ConfigDirectory;
		public static string Path => Resources.TShockConfigPath;

		public bool TryRead(out PluginConfig config)
		{
			config = Read(Path, out bool notFound);
			return !notFound || config != default;
		}
	}

	public class PluginConfig
	{
		[JsonProperty("Auth Length", DefaultValueHandling = DefaultValueHandling.Populate)]
		public int AuthLen = 12;

		[JsonIgnore]
		[JsonProperty("Ban Related Accounts", DefaultValueHandling = DefaultValueHandling.Populate)]
		public bool DiscordIDBan = false;
		[JsonIgnore]
		[JsonProperty("Discord Ban on TShock Ban", DefaultValueHandling = DefaultValueHandling.Populate)]
		public bool ShouldDiscordBan = false;

		[JsonProperty("Verified Group", DefaultValueHandling = DefaultValueHandling.Populate)]
		public string VerifiedGroup = string.Empty;
		[JsonProperty("Unverified Group", DefaultValueHandling = DefaultValueHandling.Populate)]
		public string UnverifiedGroup = string.Empty;

		[JsonProperty("Allow Discord Invite", DefaultValueHandling = DefaultValueHandling.Populate)]
		public bool AllowInvite = true;
	}
}

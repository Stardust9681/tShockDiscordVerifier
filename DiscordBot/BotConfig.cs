using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.Configuration;
using tShockDiscordVerifier.Shared;

namespace tShockDiscordVerifier.DiscordBot
{
	class BotConfigFile : ConfigFile<BotConfig>
	{
		public const string Folder = Resources.ConfigFolderName;
		public const string File = Resources.DiscordConfigFileName;
		public static string Directory => Resources.ConfigDirectory;
		public static string Path => Resources.DiscordConfigPath;

		public bool TryRead(out BotConfig config)
		{
			config = Read(Path, out bool notFound);
			return !notFound || config != default;
		}
	}

	internal class BotConfig
	{
		[JsonProperty("Discord Bot Token", DefaultValueHandling = DefaultValueHandling.Populate)]
		public string Token = string.Empty; //Bot Token to launch as

		[JsonIgnore]
		[JsonProperty("tShock Ban on Discord Ban", DefaultValueHandling = DefaultValueHandling.Populate)]
		public bool ShouldTShockBan = true;
		[JsonIgnore]
		[JsonProperty("Verification Requirements", DefaultValueHandling = DefaultValueHandling.Populate)]
		public AuthRequirement[] Requirements = Array.Empty<AuthRequirement>();
	}

	internal class AuthRequirement
	{
		[JsonProperty("Requirement Type")]
		public RequirementType Type;
		[JsonProperty("ID")]
		public ulong ID;
	}
	internal enum RequirementType
	{
		InChannel,
		NotInChannel,
		HasRole,
		NotHasRole,
	}
}

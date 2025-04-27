using System;
using System.Linq;
using System.Collections.Generic;

using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

using TerrariaApi.Server;

using Terraria;

using static SqlKata.Execution.QueryExtensions;
using Discord;

namespace tShockDiscordVerifier
{
	//File name is separate of class name.
	//Elected to name file 'Main' so it's clear where the entry point is.
	//Hope this helps.

	[ApiVersion(2, 1)]
	public class Plugin : TerrariaPlugin
	{
		//This is a fun line we have to include in ALL plugins
		public Plugin(Main game) : base(game) { }

		public override void Initialize()
		{
			Shared.Core.InitAll(this);
			if (string.IsNullOrEmpty(Shared.Core.BotConfig.Token))
			{
				TShockAPI.TShock.Log.ConsoleError($"Invalid Bot Token. Please see: '{DiscordBot.BotConfigFile.Path}'");
				Thread.Sleep(Timeout.Infinite);
				return;
			}

			while (!Shared.Core.Client.Ready) ;
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Shared.Core.DisposeAll();
			}
			base.Dispose(disposing);
		}
	}
}

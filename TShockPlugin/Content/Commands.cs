using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

using tShockDiscordVerifier.Shared;
using static tShockDiscordVerifier.Shared.Utilities;
using tShockDiscordVerifier.DiscordBot;
using SqlKata.Execution;
using Microsoft.Xna.Framework;
using Discord.Rest;

namespace tShockDiscordVerifier.TShockPlugin.Content
{
	public static class Commands
	{
		public static void Init()
		{
			TShockAPI.Commands.ChatCommands.Add(
				new Command(Verify, "verify")
				{
					HelpText = "/verify",
					HelpDesc = new string[]
					{
						"/verify",
						$"Used to verify through {Shared.Core.Client.Name} ({Gradient("/verify <auth code>", Color.LightPink, Color.LightSkyBlue)})",
					},
					AllowServer = false,
				}
			);

			TShockAPI.Commands.ChatCommands.Add(
				new Command(DiscordInvite, "invite")
				{
					HelpText = "/invite",
					HelpDesc = new string[]
					{
						"/invite",
						"Sends an invite to the affiliated Discord server"
					},
				}
			);

			TShockAPI.Commands.ChatCommands.Add(
				new Command(Permissions.user, Query, "query")
				{
					HelpText = "/query <sqlite statement>",
					HelpDesc = new string[]
					{
						"/query <sqlite statement>",
						"Run arbitrary queries through database (sqlite3)",
						"(autoincrement key int32 Id) (unique string Username) (uint64 DiscordID)"
					},
					AllowServer = true,
				}
			);
		}

		private static void RelayFault(TSPlayer player, string cmdName)
		{
			Command cmd = TShockAPI.Commands.ChatCommands.First(x => x.Names.Contains(cmdName));
			player.SendErrorMessage(
				$"Invalid or incorrect usage of command, '{cmdName}' please see '/help {cmdName}'"
				+ $"\n{string.Join("\n", cmd.HelpDesc)}"
				);
		}

		private static void Verify(CommandArgs args)
		{
			Shared.AsyncExec.Executor.Run(VerifyAsync(args));
			return;
		}
		private static async Task VerifyAsync(CommandArgs args)
		{
			await Task.Run(async () => {
				if (args.Player.Account is null)
				{
					args.Player.SendErrorMessage("You must be logged in first to verify");
					return;
				}
				if (args.Player.Group.Name.Equals(Core.PluginConfig.VerifiedGroup))
				{
					args.Player.SendErrorMessage("You are already verified!");
					return;
				}

				string name = args.Player.Account!.Name;

				if (args.Parameters.Any()) //Validate param count
				{
					RelayFault(args.Player, "verify");
					return; //Invalid param count
				}

				//args.Player.SendInfoMessage($"Constructing AUTH password. Please wait...");
				string auth = Shared.Verification.Verifier.CreateEntry(name);
				
				args.Player.SendSuccessMessage(
					$"Your authentication code is: {Gradient(auth, Color.LightPink, Color.LightSkyBlue)}" +
					$"\nUse {Gradient("/verify " + auth, Color.LightPink, Color.LightSkyBlue)} in Discord to verify."
					);
			});
		}

		private static void Query(CommandArgs args)
		{
			Shared.AsyncExec.Executor.Run(QueryAsync(args));
		}
		private static async Task QueryAsync(CommandArgs args)
		{
			try
			{
				await Core.DBHandler.DB.StatementAsync(string.Join(' ', args.Parameters));
			}
			catch (Exception x)
			{
				args.Player.SendErrorMessage(x.ToString());
			}
		}

		private static void DiscordInvite(CommandArgs args)
		{
			Shared.AsyncExec.Executor.Run(DiscordInviteAsync(args));
		}
		private static async Task DiscordInviteAsync(CommandArgs args)
		{
			if (args.Parameters.Any())
			{
				RelayFault(args.Player, "invite");
				return;
			}

			if (!Shared.Core.PluginConfig.AllowInvite)
			{
				args.Player.SendWarningMessage("This server has \"Allow Discord Invite\" disabled");
				return;
			}

			var guild = Shared.Core.Client.GetPrimaryGuild();
			if (guild is null) return;

			var invites = await guild.GetInvitesAsync();
			if (invites is null || !invites.Any()) return;

			var largestInvite = invites.Max(DiscordRestInviteMetadataComparer.Instance);

			args.Player.SendInfoMessage($"Join here! {Gradient(largestInvite!.Url, Color.LightPink, Color.LightSkyBlue)}");
		}
		private class DiscordRestInviteMetadataComparer : IComparer<Discord.Rest.RestInviteMetadata>
		{
			private static DiscordRestInviteMetadataComparer _instance;
			public static DiscordRestInviteMetadataComparer Instance
				=> _instance ??= new DiscordRestInviteMetadataComparer();
			private DiscordRestInviteMetadataComparer() { }
			public int Compare(RestInviteMetadata? a, RestInviteMetadata? b)
			{
				if (a?.Uses is null)
					return b?.Uses is null ? 0 : -1;
				if (b?.Uses is null)
					return 1;
				return a!.Uses.Value.CompareTo(b!.Uses.Value);
			}
		}
	}
}

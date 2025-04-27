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

namespace tShockDiscordVerifier.TShockPlugin
{
	public class TShockManager : IDisposable
	{
		public static readonly Identifier DiscordIdentifier = Identifier.Register("disc:", "An identifier for Discord ID");
		public static string GetDiscordBanIdentifier(ulong discID) => $"{DiscordIdentifier.Prefix}{discID}";

		public TShockManager(TerrariaPlugin plugin)
		{
			Load(plugin);
		}
		public void Dispose()
		{
			Unload();
		}

		public void Load(TerrariaPlugin plugin)
		{
			LoadHooks(plugin);
		}
		//No related "UnloadHooks" method, because when that happens, the server's been reset anyway
		private void LoadHooks(TerrariaPlugin plugin)
		{
			TShockAPI.DB.BanManager.OnBanPostAdd += TShockBanByDiscordID;
			TShockAPI.DB.BanManager.OnBanPostAdd += TShockBanOnDiscord;
			ServerApi.Hooks.ServerConnect.Register(plugin, CheckBans);
			PlayerHooks.PlayerPostLogin += CheckVerificationStatus;
		}

		public void Unload()
		{
			
		}

		internal bool TryVerify(string username)
		{
			TSPlayer? player = TShock.Players.FirstOrDefault(x => x is not null && x.Account?.Name.Equals(username) == true);
			return TryVerify(player);
		}
		internal bool TryVerify(TSPlayer? player = null)
		{
			if (Shared.Core.PluginConfig.VerifiedGroup.Equals(string.Empty) || !TShock.Groups.GroupExists(Shared.Core.PluginConfig.VerifiedGroup))
				return FailWithLog($"Verified Group ({Shared.Core.PluginConfig.VerifiedGroup}) Did Not Exist");
			if (Shared.Core.PluginConfig.UnverifiedGroup.Equals(string.Empty) || !TShock.Groups.GroupExists(Shared.Core.PluginConfig.UnverifiedGroup))
				return FailWithLog($"Unverified Group ({Shared.Core.PluginConfig.UnverifiedGroup}) Did Not Exist");

			if (player is null)
				return FailWithLog($"Player was null");
			if (player.Account is null)
				return FailWithLog($"Account was null (found {player.Name})");

			if (player.Group.Name.Equals(Shared.Core.PluginConfig.VerifiedGroup))
				return FailWithLog($"Player was already in Verified Group");
			//Don't want admins/owners to attempt to verify, and then "Oops! Actually you're a normal user now! Muah hah hah"
			if (!player.Group.Name.Equals(Shared.Core.PluginConfig.UnverifiedGroup))
				return FailWithLog($"Player is ineligible to verify (in group: {player.Account.Group})");

			//TShockAPI.Group group = TShock.Groups.GetGroupByName(TShock.Config.Settings.DefaultRegistrationGroupName);
			//player.Group.AssignTo(group);
			TShock.UserAccounts.SetUserGroup(player.Account, Shared.Core.PluginConfig.VerifiedGroup);
			return true;
		}

		internal bool TryRevokeVerification(string username)
		{
			TSPlayer? player = TShock.Players.FirstOrDefault(x => x is not null && x.Account?.Name.Equals(username) == true);
			return TryRevokeVerification(player);
		}
		internal bool TryRevokeVerification(TSPlayer? player = null)
		{
			if (Shared.Core.PluginConfig.VerifiedGroup.Equals(string.Empty) || !TShock.Groups.GroupExists(Shared.Core.PluginConfig.VerifiedGroup))
				return FailWithLog($"Verified Group ({Shared.Core.PluginConfig.VerifiedGroup}) Did Not Exist");
			if (Shared.Core.PluginConfig.UnverifiedGroup.Equals(string.Empty) || !TShock.Groups.GroupExists(Shared.Core.PluginConfig.UnverifiedGroup))
				return FailWithLog($"Unverified Group ({Shared.Core.PluginConfig.UnverifiedGroup}) Did Not Exist");

			if (player is null)
				return FailWithLog("Player was null");

			if (!player.Group.Name.Equals(Shared.Core.PluginConfig.VerifiedGroup))
				return FailWithLog($"Player {player.Name} (account:{player.Account.Name}) is not in Verified Group");

			//TShockAPI.Group group = TShock.Groups.GetGroupByName(TShock.Config.Settings.DefaultRegistrationGroupName);
			//player.Group.AssignTo(group);
			TShock.UserAccounts.SetUserGroup(player.Account, Shared.Core.PluginConfig.UnverifiedGroup);
			return true;
		}

		private const bool DEBUG = false;
		private static bool FailWithLog(string message) => ReturnWithLog(false, message);
		public static T ReturnWithLog<T>(T result, string? message = null)
		{
			if (DEBUG && !string.IsNullOrEmpty(message))
			{
				TShock.Log.ConsoleDebug(message!);
			}
			return result;
		}



		///<summary>Checks user verification status upon logging in</summary>
		private void CheckVerificationStatus(PlayerPostLoginEventArgs args)
		{
			Shared.AsyncExec.Executor.Run(Task.Run(async () => {
				IEnumerable<dynamic>? discIDs = Shared.Core.DBHandler.DB.Query("Accounts")
					.Select("DiscordID")
					.From("Accounts")
					.Where("Username", args.Player.Account.Name)
					.Get();
				if (discIDs is null || !discIDs.Any())
				{
					if (TryRevokeVerification(args.Player))
					{
						args.Player.SendWarningMessage("Unable to verify your account." +
							"\nFurthermore, you will now find your verification status revoked.");
					}
					return;
				}

				ulong discID = (ulong)discIDs!.First().DiscordID;

				Discord.WebSocket.SocketGuild? guild = Shared.Core.Client.GetPrimaryGuild();
				if (guild is null) return;

				await guild!.DownloadUsersAsync();
				IUser? user = guild!.GetUser(discID);
				if (user is null) goto FailedVerification;

				(bool failed, bool setupFail) = await Shared.Core.Client.ValidateUserAsync(user);
				if (failed) goto FailedVerification;

				if (TryVerify(args.Player))
				{
					args.Player.SendSuccessMessage("Your account was verified successfully!");
				}
				return;

			FailedVerification:
				args.Player.SendWarningMessage("Unable to verify your account.");
				if (TryRevokeVerification(args.Player))
				{
					args.Player.SendWarningMessage("Furthermore, you will now find your verification status revoked.");
				}
				return;
			}));
		}
		///<summary>Handles banning of accounts whose associated Discord ID has been banned</summary>
		private static void CheckBans(ConnectEventArgs args)
		{
			if (args.Handled)
				return;

			TSPlayer player = new TSPlayer(args.Who);

			if (player.Account is null)
				return;

			IEnumerable<dynamic>? discIDs = Shared.Core.DBHandler.DB.Query("Accounts")
				.Select("DiscordID")
				.From("Accounts")
				.Where("Username", player.Account.Name)
				.Get();
			if (discIDs is null || !discIDs.Any()) return;
			ulong discID = discIDs.First();

			Ban? ban = TShock.Bans.RetrieveBansByIdentifier(DiscordIdentifier.Prefix)?.FirstOrDefault(x => x.Identifier.EndsWith($"{discID}"));
			if (ban is not null)
			{
				player.Disconnect($"#{ban.TicketNumber} - You are banned: {ban.Reason} ({ban.GetPrettyExpirationString()} remaining)");
				args.Handled = true;
			}
		}
		///<summary>Will ban any accounts that used the same Discord ID to verify</summary>
		private static void TShockBanByDiscordID(object? sender, BanEventArgs args)
		{
			if (!Shared.Core.PluginConfig.DiscordIDBan) return;
			if (args.Ban.Identifier.StartsWith(DiscordIdentifier.Prefix)) return; //Prevent infinite recursion

			//I try to avoid var, I really do, but I think I've truly found a limit to how much I'm willing to tolerate
			//Typing the same thing over and over and over and over and over again.
			var discIDs = Shared.Core.DBHandler.DB.Query("Accounts")
				.Select("DiscordID")
				.Where("Username", args.Player.Account.Name)
				.Get();
			if (!discIDs.Any()) return;
			ulong discID = discIDs.First();

			//Prevent repeat entries
			if (!TShock.Bans.Bans.Any(x => x.Value.Identifier.StartsWith(DiscordIdentifier.Prefix)))
				TShock.Bans.InsertBan(GetDiscordBanIdentifier(discID), args.Ban.Reason, args.Ban.BanningUser, args.Ban.BanDateTime, args.Ban.ExpirationDateTime);

			var accounts = Shared.Core.DBHandler.DB.Query("Accounts")
				.Select("Username")
				.Where("DiscordID", discID)
				.Get();
			if (!accounts.Any()) return;

			foreach (string acct in accounts)
			{
				UserAccount account = TShock.UserAccounts.GetUserAccountByName(acct);

				string banIdentifier = args.Ban.Identifier;
				if (banIdentifier.Equals(Identifier.Name.Prefix))
				{
					TShock.Bans.InsertBan($"{Identifier.Name}{account.Name}", args.Ban.Reason, args.Ban.BanningUser, args.Ban.BanDateTime, args.Ban.ExpirationDateTime);
				}
				else if (banIdentifier.Equals(Identifier.Account.Prefix))
				{
					TShock.Bans.InsertBan($"{Identifier.Account}{account.Name}", args.Ban.Reason, args.Ban.BanningUser, args.Ban.BanDateTime, args.Ban.ExpirationDateTime);
				}
				else if (banIdentifier.Equals(Identifier.IP.Prefix))
				{
					TShock.Bans.InsertBan($"{Identifier.IP}{account.KnownIps}", args.Ban.Reason, args.Ban.BanningUser, args.Ban.BanDateTime, args.Ban.ExpirationDateTime);
				}
				else if (banIdentifier.Equals(Identifier.UUID.Prefix))
				{
					TShock.Bans.InsertBan($"{Identifier.UUID}{account.UUID}", args.Ban.Reason, args.Ban.BanningUser, args.Ban.BanDateTime, args.Ban.ExpirationDateTime);
				}
			}
		}
		/// <summary>If configured to ban from Discord upon tShock ban, this will ban the associated user(s) (if any exist) upon doing so</summary>
		private static void TShockBanOnDiscord(object? sender, BanEventArgs args)
		{
			if (!Shared.Core.PluginConfig.ShouldDiscordBan) return;

			Shared.AsyncExec.Executor.Run(Task.Run(async () => {
				IGuild? activeGuild = Shared.Core.Client.GetPrimaryGuild();
				if (activeGuild is null) return;

				//I try to avoid var, I really do, but I think I've truly found a limit to how much I'm willing to tolerate
				//Typing the same thing over and over and over and over and over again.
				var discIDs = Shared.Core.DBHandler.DB.Query("Accounts")
					.Select("DiscordID")
					.Where("Username", args.Player.Account.Name)
					.Get();
				if (!discIDs.Any())
					return;
				ulong discID = discIDs.First();

				IUser toBeBanned = await activeGuild.GetUserAsync(discID);

				await activeGuild.BanUserAsync(toBeBanned);
				TShock.Log.ConsoleInfo($"Banned Discord user: {toBeBanned.Username}");
			}));
		}
	}
}
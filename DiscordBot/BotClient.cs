using Discord;
using Discord.WebSocket;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static SqlKata.Execution.QueryExtensions;

namespace tShockDiscordVerifier.DiscordBot
{
	class BotClient : IDisposable
	{
		public static BotClient Instance => Shared.Core.Client;

		private DiscordSocketClient _client;
		public IDiscordClient Client => _client;

		public bool Ready { get; private set; } = false;
		public string Name { get; private set; } = string.Empty;
		internal BotClient()
		{
			_client = new DiscordSocketClient(new DiscordSocketConfig()
			{
				GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
				AlwaysDownloadUsers = true,
				MessageCacheSize = 5,
				AlwaysDownloadDefaultStickers = false,
				AlwaysResolveStickers = false,
				MaxWaitBetweenGuildAvailablesBeforeReady = 5000,
			});

			_client.UserBanned += OnUserBanned;
			_client.Ready += OnReady;
			_client.SlashCommandExecuted += CommandExec;

			Shared.AsyncExec.Executor.Run(Task.Run(async () => {
				if (string.IsNullOrEmpty(Shared.Core.BotConfig.Token))
					return;
				await _client.LoginAsync(TokenType.Bot, Shared.Core.BotConfig.Token);
				await _client.StartAsync();
			}));
		}

		public void Dispose()
		{
			Shared.AsyncExec.Executor.Run(Task.Run(async () => {
				await _client.StopAsync();
				Ready = false;
			}));
		}

		public static ulong CmdID_Verify { get; private set; }
		private async Task CommandExec(SocketSlashCommand arg)
		{
			if (arg.HasResponded) return;
			if (arg.CommandId == CmdID_Verify)
			{
				try
				{
					SocketSlashCommandDataOption param = arg.Data.Options.First();
					string auth = param.Value.ToString() ?? "";

					if (Shared.Verification.Verifier.Verify(auth, out string username))
					{
						if (Shared.Core.QueryBuilder
							.Query("Accounts")
							.Select(Shared.Resources.ColUsername)
							.Get()
							?.Any(x => x.Username is string name && name.Equals(username)) == true)
						{
							await arg.RespondAsync("### Your authentication failed!\nYou are already verified.");
							return;
						}

						await arg.RespondAsync("### Success!\nYou'll receive a message ingame upon being verified (you may have to relog)");

						Shared.Core.QueryBuilder
							.Query("Accounts")
							.Insert(new KeyValuePair<string, object>[] { new("Username", username), new("DiscordID", arg.User.Id) });
						
						if (Shared.Core.PluginHandler.TryVerify(username))
						{
							TShockAPI.TShock.Players.FirstOrDefault(x => x.Account.Name.Equals(username))
								?.SendSuccessMessage("You were successfully verified!");
						}

						return;
					}
					await arg.RespondAsync("### Your authentication failed!\nEnsure the code matches EXACTLY (caps matter)");
				}
				catch (Exception x)
				{
					TShockAPI.TShock.Log.Error(x.ToString());
				}
			}
		}

		private async Task OnReady()
		{
			TShockAPI.TShock.Log.ConsoleInfo($"Bot Connected at {DateTime.Now}");
			TShockPlugin.Content.Commands.Init();
			Name = _client.CurrentUser.GlobalName;
			Ready = true;

			SlashCommandBuilder builder = new SlashCommandBuilder();
			builder.WithName("verify");
			builder.WithDescription("Run after verifying through tShock to verify!");
			builder.AddOption("code", ApplicationCommandOptionType.String, "Code passed to you when you verify ingame", true);
			builder.WithContextTypes(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel);
			//Yo, guys who made Discord.NET
			//WHY IS NOT FLAGS ENUM??? ^^^
			//There aren't THAT many different places where commands could be run, and this would be more efficient...
			try
			{
				var cmd = await _client.CreateGlobalApplicationCommandAsync(builder.Build());
				CmdID_Verify = cmd.Id;
			}
			catch (Exception x)
			{
				TShockAPI.TShock.Log.ConsoleError(x.ToString());
			}
		}

		private static async Task OnUserBanned(SocketUser args1, SocketGuild args2)
		{
			//Thanks, Discord.NET
			//GetAuditLogsAsync(..) PAGINATES results. Internally.
			//So you get basically a list of lists. Which is really weird.
			//Like it makes some sense, but guuuuurl I'm just getting 3 entries to make sure there's no weird shit going on
			//I don't need PAGES (each page has 100 entries total btw)
			IAsyncEnumerable<Discord.Rest.RestAuditLogEntry> bans = args2.GetAuditLogsAsync(3, actionType: ActionType.Ban).Flatten();
			Discord.Rest.RestAuditLogEntry? entry = await bans.FirstOrDefaultAsync(x => !x.User.IsBot && ((SocketBanAuditLogData)x.Data).Target.DownloadAsync().Result.Id.Equals(args1.Id));
			if (entry is null)
				return;

			if (Shared.Core.BotConfig.ShouldTShockBan)
			{
				//Names of all the accounts using the same Discord ID
				IEnumerable<dynamic> names = Shared.Core.DBHandler.DB.Query("Accounts")
					.Select("Username")
					.Where(new KeyValuePair<string, object>[] { new("DiscordID", args1.Id) })
					.Get();

				//TShock literally just has null admin name if there's no associated user :/
				string? banner = (string)Shared.Core.DBHandler.DB.Query("Accounts")
					.Select("Username")
					.Where(new KeyValuePair<string, object>[] { new("DiscordID", entry.User.Id) })
					.FirstOrDefault();

				//Ban each user from account names
				foreach(string accountName in names)
				{
					TShockAPI.TShock.Bans.InsertBan(TShockPlugin.TShockManager.GetDiscordBanIdentifier(args1.Id), "Banned from Discord", banner, DateTime.UtcNow, DateTime.MaxValue);
					TShockAPI.TShock.Log.ConsoleInfo($"Banned {accountName} with Identifier:{TShockPlugin.TShockManager.GetDiscordBanIdentifier(args1.Id)} permanently");
				}
			}
		}

		public void ValidateUser(IUser user, out bool failed, out bool wasSetupFail)
		{
			(failed, wasSetupFail) = ValidateUserAsync(user).GetAwaiter().GetResult();
		}
		public async Task<(bool, bool)> ValidateUserAsync(IUser user)
		{
			if (Shared.Core.BotConfig.Requirements is null || Shared.Core.BotConfig.Requirements.Length == 0)
				return (false, false);

			bool failed = false;
			bool wasSetupFail = false;
			SocketGuild? guild = GetPrimaryGuild();
			IGuildUser member;
			IChannel channel;
			foreach (AuthRequirement req in Shared.Core.BotConfig.Requirements)
			{
				switch (req.Type)
				{
					case RequirementType.InChannel:
						channel = await Client.GetChannelAsync(req.ID);
						if (channel is null)
						{
							failed = true; //Did not find channel
							wasSetupFail = true;
						}
						else if (channel.GetUserAsync(user.Id).Result is null) failed = true; //No perms
						break;
					case RequirementType.NotInChannel:
						channel = await Client.GetChannelAsync(req.ID);
						if (channel is null)
						{
							failed = true; //Did not find channel
							wasSetupFail = true;
						}
						else if (channel.GetUserAsync(user.Id).Result is not null) failed = true; //No perms
						break;

					case RequirementType.HasRole:
						if (guild is null)
						{
							failed = true; //Did not find guild
							wasSetupFail = true;
							break;
						}
						member = guild.GetUser(user.Id);
						if (member is null)
						{
							failed = true; //Did not find user
							break;
						}
						if (!member.RoleIds.Contains(req.ID)) failed = true; //No perms
						break;

					case RequirementType.NotHasRole:
						if (guild is null)
						{
							failed = true; //Did not find guild
							wasSetupFail = true;
							break;
						}
						member = guild.GetUser(user.Id);
						if (member is null)
						{
							failed = true; //Did not find user
							break;
						}
						if (member.RoleIds.Contains(req.ID)) failed = true; //No perms
						break;
				}

				if (failed)
					break;
			}
			return (failed, wasSetupFail);
		}

		public SocketGuild? GetPrimaryGuild() => _client.Guilds.FirstOrDefault();
	}
}

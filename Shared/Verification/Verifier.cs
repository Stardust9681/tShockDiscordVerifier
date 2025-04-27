using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using SqlKata.Execution;

namespace tShockDiscordVerifier.Shared.Verification
{
	using KVP = KeyValuePair<string, AuthEntry>;
	public class Verifier
	{
		private static int AuthLength => Shared.Core.PluginConfig.AuthLen;
		private static string CreateString() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(Math.Max(1, AuthLength)));
		private static readonly ConcurrentDictionary<string, AuthEntry> AuthEntries = new ConcurrentDictionary<string, AuthEntry>();

		public static string CreateEntry(string IGN)
		{
			if(AuthEntries.Values.Any(x=>x.Username.Equals(IGN)))
				_ = AuthEntries.TryRemove(AuthEntries.First(x => x.Value.Username.Equals(IGN)));

			string auth = "";
			do //I CANNOT BELIEVE I FOUND A USE FOR DO-WHILE
				auth = CreateString();
			while (!AuthEntries.TryAdd(auth, new AuthEntry(IGN)));

			return auth;
		}

		public static bool Verify(string auth, out string username)
		{
			bool success = AuthEntries.TryRemove(auth, out AuthEntry entry);
			username = success ? entry.Username : string.Empty;

			foreach (KVP pair in AuthEntries)
			{
				if (pair.Value.Expired)
					AuthEntries.TryRemove(pair);
			}

			return success;
		}
	}
	internal struct AuthEntry
	{
		public readonly string Username;
		private readonly DateTime CreationDate;
		public bool Expired => DateTime.Now.Subtract(CreationDate) > TimeSpan.FromMinutes(5);

		public AuthEntry()
		{
			Username = "";
			CreationDate = DateTime.Now;
		}
		public AuthEntry(string username)
		{
			Username = username;
			CreationDate = DateTime.Now;
		}
	}
}

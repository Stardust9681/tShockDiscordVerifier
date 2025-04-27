using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;

namespace tShockDiscordVerifier.Shared
{
	public static class Utilities
	{
		public static string Gradient(string input, Color start, Color end, int steps = 5)
		{
			int len = input.Length;
			if (steps > len)
				steps = len;

			int size = len / steps;

			string output = "";
			for (int i = 0; i < len; i += size)
			{
				if (i + size > len) size = len - i;
				output += $"[c/{Color.Lerp(start, end, (float)i / (float)len).Hex3()}:{input.Substring(i, size)}]";
			}

			return output;
		}
	}
}

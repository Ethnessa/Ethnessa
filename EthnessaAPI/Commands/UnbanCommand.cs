using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Database;

namespace EthnessaAPI.ServerCommands
{
	public class UnbanCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "unban" };
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.ban };
		public override CommandDelegate CommandDelegate { get; set; } = Unban;

		private static void Unban(CommandArgs args)
		{
			var input = args.Parameters.ElementAtOrDefault(0);
			if(input is null)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /unban <name/ip/uuid/ban id>");
				return;
			}

			try
			{
				var removed = BanManager.RemoveBan(input);
				if(removed is false)
				{
					args.Player.SendErrorMessage($"Failed to unban '{input}'.");
					return;
				}
				else
				{
					args.Player.SendSuccessMessage($"Unbanned '{input}'.");
				}
			}catch(Exception ex)
			{
				args.Player.SendErrorMessage($"Failed to unban '{input}'. Exception: {ex.Message}");
				ServerBase.Log.ConsoleError($"Failed to unban '{input}'. Exception: {ex.ToString()}");
			}
		}
	}
}

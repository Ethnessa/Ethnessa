using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Models;

namespace EthnessaAPI.ServerCommands
{
	public class UserInfoCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "userinfo", "ui" };
		public override CommandDelegate CommandDelegate { get; set; } = GrabUserUserInfo;
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.userinfo };
		private static void GrabUserUserInfo(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}userinfo <player>.", Commands.Specifier));
			}

			var players = ServerPlayer.GetByNameOrId(args.Parameters[0]);
			if (players.Count < 1)
				args.Player.SendErrorMessage(GetString("Invalid player."));
			else if (players.Count > 1)
				args.Player.SendMultipleMatchError(players.Select(p => p.Name));
			else
			{
				args.Player.SendSuccessMessage(GetString($"IP Address: {players[0].IP}."));
				if (players[0].Account != null && players[0].IsLoggedIn)
					args.Player.SendSuccessMessage(GetString($" -> Logged-in as: {players[0].Account.Name}; in group {players[0].Group.Name}."));
			}
		}
	}
}

using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using EthnessaAPI.Models;

namespace EthnessaAPI.ServerCommands
{
	public class KickCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "kick" };
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.kick };
		public override CommandDelegate CommandDelegate { get; set; } = Kick;


		private static void Kick(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}kick <player> [reason].", Commands.Specifier));
				return;
			}
			if (args.Parameters[0].Length == 0)
			{
				args.Player.SendErrorMessage(GetString("A player name must be provided to kick a player. Please provide one."));
				return;
			}

			string plStr = args.Parameters[0];
			var players = ServerPlayer.GetByNameOrId(plStr);
			if (players.Count == 0)
			{
				args.Player.SendErrorMessage(GetString("Player not found. Unable to kick the player."));
			}
			else if (players.Count > 1)
			{
				args.Player.SendMultipleMatchError(players.Select(p => p.Name));
			}
			else
			{
				string reason = args.Parameters.Count > 1
									? String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1))
									: GetString("Misbehaviour.");
				if (!players[0].Kick(reason, !args.Player.RealPlayer, false, args.Player.Name))
				{
					args.Player.SendErrorMessage(GetString("You can't kick another admin."));
				}
			}
		}

	}
}

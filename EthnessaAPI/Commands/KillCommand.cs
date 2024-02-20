using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Models;

namespace EthnessaAPI.ServerCommands;

public class KillCommand : Command
{
	public override List<string> Names { get; protected set; } = new() { "kill" };
	public override CommandDelegate CommandDelegate { get; set; } = KillPlayer;
	public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.kill };

	private static void KillPlayer(CommandArgs args)
	{
		var target = args.Parameters.ElementAtOrDefault(0);
		if (target is null)
		{
			args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}kill <player name>.", Commands.Specifier));
			return;
		}

		var player = ServerPlayer.GetFirstByNameOrId(target);
		if (player is null)
		{
			args.Player.SendErrorMessage(GetString("Player {0} not found.", target));
			return;
		}

		player.KillPlayer();
		args.Player.SendSuccessMessage(GetString("Player {0} has been killed.", target));
	}
}

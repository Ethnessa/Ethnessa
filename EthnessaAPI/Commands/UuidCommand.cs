using System.Collections.Generic;
using System.Linq;

namespace EthnessaAPI.ServerCommands;

public class UuidCommand : Command
{
	public override List<string> Names { get; protected set; } = new(){ "uuid" };
	public override CommandDelegate CommandDelegate { get; set; } = Uuid;
	public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.advaccountinfo };
	private static void Uuid(CommandArgs args)
	{
		var player = args.Player;
		var input = args.Parameters.ElementAtOrDefault(0);

		if (input is null)
		{
			if (player.RealPlayer is false)
			{
				player.SendErrorMessage("You must specify a player.");
				return;
			}

			player.SendSuccessMessage($"Your UUID is: {player.UUID}");
			return;
		}

		var target = ServerPlayer.GetFirstByNameOrId(input);
		if (target is null)
		{
			player.SendErrorMessage("No players matched your query.");
			return;
		}

		player.SendSuccessMessage($"{target.Name}'s UUID is: {target.UUID}");
	}
}

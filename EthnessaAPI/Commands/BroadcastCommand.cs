using System.Collections.Generic;

namespace EthnessaAPI.ServerCommands;

public class BroadcastCommand : Command
{
	public override List<string> Names { get; protected set; } = new() { "bc", "broadcast" };
	public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.broadcast };
	public override CommandDelegate CommandDelegate { get; set; } = Broadcast;
	private static void Broadcast(CommandArgs args)
	{
		if (args.Parameters.Count == 0)
		{
			args.Player.SendErrorMessage("Usage: /bc <message>");
			return;
		}

		var message = string.Join(" ", args.Parameters);
		ServerBase.Utils.Broadcast($"{message}", 255, 255, 255);
	}
}

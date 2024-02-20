using System;
using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Database;
using EthnessaAPI.Models;

namespace EthnessaAPI.ServerCommands;

public class MuteCommand : Command
{
	public override List<string> Names { get; protected set; } = new() { "mute" };
	public override CommandDelegate CommandDelegate { get; set; } = Mute;
	public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.mute };

	private static void Mute(CommandArgs args)
	{
		var player = args.Player;
		var targetInput = args.Parameters.ElementAtOrDefault(0);

		if (targetInput is null)
		{
			player.SendErrorMessage("Invalid syntax! Proper syntax: /mute <player> (time)");
			return;
		}

		var targetPlayer = ServerPlayer.GetFirstByNameOrId(targetInput ?? "");
		if (targetPlayer is null)
		{
			player.SendErrorMessage("Invalid player!");
			return;
		}

		if (MuteManager.IsPlayerMuted(targetPlayer))
		{
			player.SendErrorMessage("Player is already muted!");
			return;
		}

		var expiryInput = args.Parameters.ElementAtOrDefault(1) ?? "-1"; // perma mute by default


		DateTime expiry;

		if (expiryInput is "-1")
		{
			expiry = DateTime.MaxValue;
		}
		else
		{
			var timespan = ServerBase.Utils.ParseDuration(expiryInput);
			if (timespan is null)
			{
				player.SendErrorMessage("Invalid duration format. Please follow the format: 1d2h3m4s");
				return;
			}
			expiry = DateTime.Now + timespan.Value;
		}

		var success = MuteManager.MutePlayer(targetPlayer);
		if (success)
		{
			ServerPlayer.All.SendInfoMessage($"{targetPlayer.Name} has been muted by {player.Name} until {expiry}");
		}
		else
		{
			player.SendErrorMessage("Failed to mute player. Please try again.");
		}
	}
}

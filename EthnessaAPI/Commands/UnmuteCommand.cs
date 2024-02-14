using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Database;

namespace EthnessaAPI.ServerCommands
{
	public class UnmuteCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "unmute" };
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.mute };
		public override CommandDelegate CommandDelegate { get; set; } = Unmute;

		private static void Unmute(CommandArgs args)
		{
			var input = args.Parameters.ElementAtOrDefault(0);
			if(input is null)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /unmute <name/ip/uuid>");
				return;
			}

			var targetPlayer = ServerPlayer.GetFirstByNameOrId(input);
			if(targetPlayer is not null)
			{
				var success = MuteManager.UnmutePlayer(targetPlayer);
				if(success is false)
				{
					args.Player.SendErrorMessage($"Failed to unmute '{targetPlayer.Name}'.");
					return;
				}
				else
				{
					args.Player.SendSuccessMessage($"Unmuted '{targetPlayer.Name}'.");
				}
			}

			var removed = MuteManager.RemoveMute(input);
			if(removed is false)
			{
				args.Player.SendErrorMessage($"Failed to unmute '{input}'.");
				return;
			}
			else
			{
				args.Player.SendSuccessMessage($"Unmuted '{input}'.");
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Models;
using Terraria;

namespace EthnessaAPI.ServerCommands
{
	public class StopCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "stop" };
		public override CommandDelegate CommandDelegate { get; set; } = Off;
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.maintenance };
		private static void Off(CommandArgs args)
		{

			if (Main.ServerSideCharacter)
			{
				foreach (ServerPlayer player in ServerBase.Players)
				{
					if (player != null && player.IsLoggedIn && !player.IsDisabledPendingTrashRemoval)
					{
						player.SaveServerCharacter();
					}
				}
			}

			string reason = ((args.Parameters.Count > 0) ? GetString("Server shutting down: ") + String.Join(" ", args.Parameters) : GetString("Server shutting down!"));
			ServerBase.Utils.StopServer(true, reason);
		}

	}
}

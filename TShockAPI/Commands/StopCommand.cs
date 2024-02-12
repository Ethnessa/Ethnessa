using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace TShockAPI.ServerCommands
{
	public class StopCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "stop" };
		public override CommandDelegate CommandDelegate { get; set; } = Off;
		public override List<string> Permissions { get; protected set; } = new() { TShockAPI.Permissions.maintenance };
		private static void Off(CommandArgs args)
		{

			if (Main.ServerSideCharacter)
			{
				foreach (ServerPlayer player in TShock.Players)
				{
					if (player != null && player.IsLoggedIn && !player.IsDisabledPendingTrashRemoval)
					{
						player.SaveServerCharacter();
					}
				}
			}

			string reason = ((args.Parameters.Count > 0) ? GetString("ServerConsole shutting down: ") + String.Join(" ", args.Parameters) : GetString("ServerConsole shutting down!"));
			TShock.Utils.StopServer(true, reason);
		}

	}
}

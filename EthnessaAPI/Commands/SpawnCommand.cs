using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Models;
using Terraria;

namespace EthnessaAPI.ServerCommands
{
	public class SpawnCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string> { "spawn" };
		public override CommandDelegate CommandDelegate { get; set; } = Spawn;
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.spawn };
		private static void Spawn(CommandArgs args)
		{
			if (args.Player.Teleport(Main.spawnTileX * 16, (Main.spawnTileY * 16) - 48))
				args.Player.SendSuccessMessage(GetString("Teleported to the map's spawn point."));
		}
	}
}

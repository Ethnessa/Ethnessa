using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Models;
using Terraria;
using EthnessaAPI.ServerCommands;

namespace EthnessaAPI.ServerCommands
{
	public class SetSpawnCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string> { "setspawn" };
		public override CommandDelegate CommandDelegate { get; set; } = SetSpawn;
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.worldspawn };

		private static void SetSpawn(CommandArgs args)
		{
			Main.spawnTileX = args.Player.TileX + 1;
			Main.spawnTileY = args.Player.TileY + 3;
			SaveManager.Instance.SaveWorld(false);
			args.Player.SendSuccessMessage(GetString("Spawn has now been set at your location."));
		}
	}
}

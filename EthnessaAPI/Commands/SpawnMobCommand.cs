using System;
using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Localization;

namespace EthnessaAPI.ServerCommands
{
	internal class SpawnMobCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "spawnmob", "sm" };
		public override List<string> Permissions { get; protected set; } = new(){ EthnessaAPI.Permissions.spawnmob };
		public override CommandDelegate CommandDelegate { get; set; } = SpawnMob;
		private static void SpawnMob(CommandArgs args)
		{
			var mob = args.Parameters[0];
			var amount = 1;
			if (args.Parameters.Count > 1)
			{
				if (!int.TryParse(args.Parameters[1], out amount))
				{
					args.Player.SendErrorMessage("Invalid amount.");
					return;
				}
			}

			// check if mob is valid
			var foundMobs = ServerBase.Utils.GetNPCByIdOrName(mob);

			if (foundMobs.Count == 1)
			{
				var npc = foundMobs.First();

				ServerPlayer.ServerConsole.SpawnNPC(npc.netID, npc.FullName, amount,args.Player.TileX, args.Player.TileY, 50,20);
				if (args.Silent)
				{
					args.Player.SendSuccessMessage($"Spawned {amount} {npc.FullName}({npc.type}) silently.");
				}
				else
				{
					ServerPlayer.All.SendSuccessMessage($"{args.Player.Name} has spawned {amount} {npc.FullName}");
				}
			}
			else if (foundMobs.Count > 1)
			{
				args.Player.SendMultipleMatchError(foundMobs.Select(n => $"{n.FullName}({n.type})"));
				return;
			}
			else
			{
				args.Player.SendErrorMessage("Invalid mob.");
				return;
			}
		}
	}
}

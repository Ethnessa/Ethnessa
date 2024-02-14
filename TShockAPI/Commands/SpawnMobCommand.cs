using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI.Localization;

namespace TShockAPI.ServerCommands
{
	internal class SpawnMobCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "spawnmob", "sm" };
		public override List<string> Permissions { get; protected set; } = new(){ TShockAPI.Permissions.spawnmob };
		public override CommandDelegate CommandDelegate { get; set; } = SpawnMob;

		public static void SpawnMob(CommandArgs args)
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
				
			}else if (foundMobs.Count > 1)
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

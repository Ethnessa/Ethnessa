using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Models;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace EthnessaAPI.ServerCommands;

public class ButcherCommand : Command
{
	public override List<string> Names { get; protected set; } = new() { "butcher", "killall", "slaughter" };
	public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.butcher };
	public override CommandDelegate CommandDelegate { get; set; } = Butcher;

	private static void Butcher(CommandArgs args)
	{
		var user = args.Player;
		if (args.Parameters.Count > 1)
		{
			user.SendMessage(GetString("Butcher Syntax and Example"), Color.White);
			user.SendMessage(GetString($"{"butcher".Color(Utils.BoldHighlight)} [{"NPC name".Color(Utils.RedHighlight)}|{"ID".Color(Utils.RedHighlight)}]"), Color.White);
			user.SendMessage(GetString($"Example usage: {"butcher".Color(Utils.BoldHighlight)} {"pigron".Color(Utils.RedHighlight)}"), Color.White);
			user.SendMessage(GetString("All alive NPCs (excluding town NPCs) on the server will be killed if you do not input a name or ID."), Color.White);
			user.SendMessage(GetString($"To get rid of NPCs without making them drop items, use the {"clear".Color(Utils.BoldHighlight)} command instead."), Color.White);
			user.SendMessage(GetString($"To execute this command silently, use {ServerBase.Config.Settings.CommandSilentSpecifier.Color(Utils.GreenHighlight)} instead of {ServerBase.Config.Settings.CommandSpecifier.Color(Utils.RedHighlight)}"), Color.White);
			return;
		}

		int npcId = 0;

		var userInput = args.Parameters.ElementAtOrDefault(0);

		if (userInput is not null)
		{
			var npcs = ServerBase.Utils.GetNPCByIdOrName(args.Parameters[0]);
			if (npcs.Any() is false)
			{
				user.SendErrorMessage(GetString($"\"{args.Parameters[0]}\" is not a valid NPC."));
				return;
			}

			if (npcs.Count > 1)
			{
				user.SendMultipleMatchError(npcs.Select(n => $"{n.FullName}({n.type})"));
				return;
			}
			npcId = npcs[0].netID;
		}

		int kills = 0;
		for (int i = 0; i < Main.npc.Length; i++)
		{
			if (!Main.npc[i].active || ((npcId != 0 || Main.npc[i].townNPC || Main.npc[i].netID == NPCID.TargetDummy) &&
			                            Main.npc[i].netID != npcId)) continue;

			ServerPlayer.ServerConsole.StrikeNPC(i, (int)(Main.npc[i].life + (Main.npc[i].defense * 0.6)), 0, 0);
			kills++;
		}

		if (args.Silent)
			user.SendSuccessMessage(GetPluralString("You butchered {0} NPC.", "You butchered {0} NPCs.", kills, kills));
		else
			ServerPlayer.All.SendInfoMessage(GetPluralString("{0} butchered {1} NPC.", "{0} butchered {1} NPCs.", kills, user.Name, kills));	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Models;
using Terraria;

namespace EthnessaAPI.ServerCommands
{
	public class LogoutCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string>() {"logout" };
		public override CommandDelegate CommandDelegate { get; set; } = Logout;
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.canlogout };
		public override bool AllowServer { get; set; } = false;
		public override bool DoLog { get; set; } = false;

		private static void Logout(CommandArgs args)
		{
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage(GetString("You are not logged-in. Therefore, you cannot logout."));
				return;
			}

			if (args.Player.TPlayer.talkNPC != -1)
			{
				args.Player.SendErrorMessage(GetString("Please close NPC windows before logging out."));
				return;
			}

			args.Player.Logout();
			args.Player.SendSuccessMessage(GetString("You have been successfully logged out of your account."));
			if (Main.ServerSideCharacter)
			{
				args.Player.SendWarningMessage(GetString("Server side characters are enabled. You need to be logged-in to play."));
			}
		}
	}
}

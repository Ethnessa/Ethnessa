using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Database;
using EthnessaAPI.Database.Models;

namespace EthnessaAPI.ServerCommands
{
	internal class PrefixCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string> { "prefix" };
		public override List<string> Permissions { get; protected set; } = new List<string> { EthnessaAPI.Permissions.canmanageprefixes };
		public override bool AllowServer { get; set; } = false;
		public override CommandDelegate CommandDelegate { get; set; } = ManagePrefixes;

		private static void ManagePrefixes(CommandArgs args)
		{
			var subcmd = args.Parameters.ElementAtOrDefault(0);
			var player = args.Player;

			switch (subcmd)
			{
				case "list":
				{
					var tags = UserAccountManager.GetTags(player.Account);

					player.SendInfoMessage("You have access to the following tags:");
					foreach (var tag in tags)
					{
						var status = player.Account.TagStatuses.FirstOrDefault(x => x.Name == tag.Name);
						if (status is null)
						{
							continue;
						}

						player.SendInfoMessage($"{tag.FormattedText} - {tag.Name} - {(status.Enabled ? "Enabled" : "Disabled")}");
					}
					player.SendInfoMessage("Use /tag toggle <name> to toggle a tag on or off.");
					return;
				}
				case "toggle":
				{
					var tagToToggle = args.Parameters.ElementAtOrDefault(1);
					if (tagToToggle is null)
					{
						player.SendErrorMessage("You must specify a tag to toggle. Use /tag list to see available tags.");
						return;
					}

					var tag = UserAccountManager.GetTags(player.Account).FirstOrDefault(x => x.Name == tagToToggle);

					if (tag is null)
					{
						player.SendErrorMessage("That tag either doesn't exist or you don't have access to it.");
						return;
					}

					var status = player.Account.TagStatuses.FirstOrDefault(x => x.Name == tag.Name);
					if (status is null)
					{
						status = new TagStatus(tag.Name, true);
						player.Account.TagStatuses.Add(status);
					}
					else
					{
						status.Enabled = !status.Enabled;
					}

					UserAccountManager.SaveAccount(player.Account);
					player.SendSuccessMessage($"You have {(status.Enabled ? "enabled" : "disabled")} the tag {tag.FormattedText}.");
					return;
				}
				default:
				{
					// Show help
					player.SendInfoMessage("Available subcommands:");
					player.SendInfoMessage("/tag list - List all available tags.");
					player.SendInfoMessage("/tag toggle <name> - Toggle a tag on or off.");
					return;
				}
			}
		}
	}
}

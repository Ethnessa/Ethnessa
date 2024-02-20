using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Database;
using EthnessaAPI.Models;

namespace EthnessaAPI.ServerCommands;

public class TagCommand : Command
{
	public override List<string> Names { get; protected set; } = new List<string> { "tag" };
	public override List<string> Permissions { get; protected set; } = new List<string> { EthnessaAPI.Permissions.managetags };

	public override CommandDelegate CommandDelegate { get; set; } = ManageTags;

	private static void ManageTags(CommandArgs args)
	{
		var subcmd = args.Parameters.ElementAtOrDefault(0);
		var player = args.Player;

		switch (subcmd)
		{
			case "create":
			case "add":
			{
				var tagName = args.Parameters.ElementAtOrDefault(1);
				var tagText = args.Parameters.ElementAtOrDefault(2);

				if(tagName is null || tagText is null)
				{
					player.SendErrorMessage("You must provide a tag name and tag text. Follow the format: /tag create <name> <tag text>");
					return;
				}

				if (TagManager.CreateTag(tagName, tagText))
				{
					player.SendSuccessMessage($"Tag '{tagName}' has been created, it will display as: {tagText}");
				}
				else
				{
					player.SendErrorMessage("A tag with that name already exists.");
				}

				return;
			}
			case "del":
			case "delete":
			{
				var tagName = args.Parameters.ElementAtOrDefault(1);
				if(tagName is null)
				{
					player.SendErrorMessage("You must provide a tag name to delete.");
					return;
				}

				if (TagManager.DeleteTag(tagName))
				{
					player.SendSuccessMessage($"The tag '{tagName}' has been deleted.");
				}
				else
				{
					player.SendErrorMessage("A tag with that name does not exist.");
				}

				return;
			}
			case "change":
			case "modify":
			case "update":
			{
				var tagName = args.Parameters.ElementAtOrDefault(1);
				var tagText = args.Parameters.ElementAtOrDefault(2);

				if(tagName is null || tagText is null)
				{
					player.SendErrorMessage("You must provide a tag name and tag text. Follow the format: /tag change <name> <tag text>");
					return;
				}

				if (TagManager.UpdateTag(tagName, tagText))
				{
					player.SendSuccessMessage($"The tag '{tagName}' has been updated, it will display as: {tagText}");
				}
				else
				{
					player.SendErrorMessage("A tag with that name does not exist.");
				}

				return;
			}
			case "give":
			{
				var tagName = args.Parameters.ElementAtOrDefault(1);
				var targetPlayerName = args.Parameters.ElementAtOrDefault(2);
				if (tagName is null || targetPlayerName is null)
				{
					player.SendErrorMessage("You must provide a tag name and a player name. Follow the format: /tag give <name> <player>");
					return;
				}

				var targetPlayer = ServerPlayer.GetFirstByNameOrId(targetPlayerName);
				if(targetPlayer is null)
				{
					player.SendErrorMessage("The player you specified does not exist.");
					return;
				}

				if (targetPlayer.IsLoggedIn is false)
				{
					player.SendErrorMessage("The player you specified is not logged in.");
					return;
				}

				var tag = TagManager.GetTag(tagName);
				if (tag is null)
				{
					player.SendErrorMessage("A tag with that name does not exist.");
					return;
				}

				if (UserAccountManager.AddTag(targetPlayer.Account, tagName))
				{
					player.SendSuccessMessage($"The tag '{tagName}' has been given to {targetPlayer.Name}.");
					return;
				}

				return;
			}
			default:
			{
				player.SendInfoMessage("Tag command usage:");
				player.SendInfoMessage("/tag create <name> <tag text> - Creates a new tag");
				player.SendInfoMessage("/tag delete <name> - Deletes a tag");
				player.SendInfoMessage("/tag change <name> <tag text> - Updates a tag");
				player.SendInfoMessage("/tag give <name> <player> - Gives a tag to a player");
				return;
			}
		}
	}
}

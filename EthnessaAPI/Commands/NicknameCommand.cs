using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Database;
using EthnessaAPI.Database.Models;

namespace EthnessaAPI.ServerCommands;

public class NicknameCommand : Command
{
	public override List<string> Names { get; protected set; } = new() { "nick", "nickname","rename" };
	public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.canchangenickname };
	public override bool AllowServer { get; set; } = false;
	public override CommandDelegate CommandDelegate { get; set; } = Nickname;

	private static void Nickname(CommandArgs args)
	{
		var player = args.Player;
		var maxNickLength = ServerBase.Config.Settings.MaxNicknameLength;

		if (args.Parameters.Count == 2)
		{
			var targetPlayerInput = args.Parameters.ElementAtOrDefault(0);
			var newName = args.Parameters.ElementAtOrDefault(1);

			if (targetPlayerInput is null || newName is null)
			{
				player.SendErrorMessage($"Please provide a valid player and a new name. Example: /nick <player> <newname>");
				return;
			}

			var targetPlayer = ServerPlayer.GetFirstByNameOrId(targetPlayerInput);
			UserAccount? targetAccount = null;
			if (targetPlayer is null)
			{
				targetAccount = UserAccountManager.GetUserAccountByName(targetPlayerInput);
				if (targetAccount is null)
				{
					player.SendErrorMessage("No player found with that name.");
					return;
				}
			}
			else if (targetPlayer.IsLoggedIn is false)
			{
				player.SendErrorMessage($"'{targetPlayer.Name}' needs to be logged in to change their nickname.");
				return;
			}
			else
			{
				targetAccount = targetPlayer.Account;
			}

			if (newName is "clear")
			{
				NicknameManager.ClearNickname(targetAccount);
				player.SendSuccessMessage($"Cleared the nickname for '{targetAccount.Name}'.");
				return;
			}

			if (string.IsNullOrWhiteSpace(newName))
			{
				player.SendErrorMessage($"A nickname cannot contain only whitespace.");
				return;
			}

			if (ServerBase.Utils.ContainsFilteredWord(newName))
			{
				player.SendErrorMessage($"The nickname '{newName}' contains a filtered word.");
				return;
			}

			if (newName.Length > maxNickLength)
			{
				player.SendErrorMessage($"Nicknames cannot be longer than {maxNickLength} characters.");
				return;
			}

			if (NicknameManager.SetNickname(targetAccount, newName))
			{
				player.SendSuccessMessage($"Set {targetAccount.Name}'s nickname to '{newName}'");
				return;
			}
			else
			{
				player.SendErrorMessage($"Failed to set {targetAccount.Name}'s nickname to '{newName}'");
				return;
			}



		}
		else if (args.Parameters.Count == 1)
		{
			// set the user executing the command to the nickname
			var newName = args.Parameters.ElementAtOrDefault(0);

			if (newName is "clear")
			{
				NicknameManager.ClearNickname(player.Account);
				player.SendSuccessMessage("Cleared your nickname.");
				return;
			}

			if (string.IsNullOrWhiteSpace(newName))
			{
				player.SendErrorMessage("A nickname cannot contain only whitespace.");
				return;
			}

			if(newName.Length > maxNickLength)
			{
				player.SendErrorMessage($"Nicknames cannot be longer than {maxNickLength} characters.");
				return;
			}

			if (ServerBase.Utils.ContainsFilteredWord(newName))
			{
				player.SendErrorMessage($"That nickname contains a filtered word.");
				return;
			}

			if (NicknameManager.SetNickname(player.Account, newName))
			{
				player.SendSuccessMessage("You have set your nickname to '{0}'", newName);
				return;
			}
			else
			{
				player.SendErrorMessage($"Something went wrong setting your nickname to '{newName}'");
				return;
			}
		}
		else
		{
			player.SendInfoMessage("Proper syntax: /nick <newname> or /nick <player> <newname>");
		}
	}
}

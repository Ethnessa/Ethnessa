using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.Database.Models;
using TShockAPI.Database;
using TShockAPI.ServerCommands;

namespace TShockAPI.ServerCommands
{
	public class UserCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "user" };
		public override CommandDelegate CommandDelegate { get; set; } = Execute;
		public override List<string> Permissions { get; protected set; } = new() { TShockAPI.Permissions.user };
		private static void Execute(CommandArgs args)
		{
			// This guy needs to be here so that people don't get exceptions when they type /user
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage(GetString("Invalid user syntax. Try {0}user help.", Commands.Specifier));
				return;
			}

			string subcmd = args.Parameters[0];

			// Add requires a username, password, and a group specified.
			if (subcmd == "add" && args.Parameters.Count == 4)
			{
				var account = new UserAccount();

				account.Name = args.Parameters[1];
				try
				{
					account.CreateBCryptHash(args.Parameters[2]);
				}
				catch (ArgumentOutOfRangeException)
				{
					args.Player.SendErrorMessage(GetString("Password must be greater than or equal to {0} characters.", ServerBase.Config.Settings.MinimumPasswordLength));
					return;
				}
				account.Group = args.Parameters[3];

				try
				{
					UserAccountManager.AddUserAccount(account);
					args.Player.SendSuccessMessage(GetString("Account {0} has been added to group {1}.", account.Name, account.Group));
					ServerBase.Log.ConsoleInfo(GetString("{0} added account {1} to group {2}.", args.Player.Name, account.Name, account.Group));
				}
				catch (UserAccountManager.GroupNotExistsException)
				{
					args.Player.SendErrorMessage(GetString("Group {0} does not exist.", account.Group));
				}
				catch (UserAccountManager.UserAccountExistsException)
				{
					args.Player.SendErrorMessage(GetString("User {0} already exists.", account.Name));
				}
				catch (UserAccountManager.UserAccountManagerException e)
				{
					args.Player.SendErrorMessage(GetString("User {0} could not be added, check console for details.", account.Name));
					ServerBase.Log.ConsoleError(e.ToString());
				}
			}
			// User deletion requires a username
			else if (subcmd == "del" && args.Parameters.Count == 2)
			{
				var account = new UserAccount();
				account.Name = args.Parameters[1];

				try
				{
					UserAccountManager.RemoveUserAccount(account);
					args.Player.SendSuccessMessage(GetString("Account removed successfully."));
					ServerBase.Log.ConsoleInfo(GetString("{0} successfully deleted account: {1}.", args.Player.Name, args.Parameters[1]));
				}
				catch (UserAccountManager.UserAccountNotExistException)
				{
					args.Player.SendErrorMessage(GetString("The user {0} does not exist! Therefore, the account was not deleted.", account.Name));
				}
				catch (UserAccountManager.UserAccountManagerException ex)
				{
					args.Player.SendErrorMessage(ex.Message);
					ServerBase.Log.ConsoleError(ex.ToString());
				}
			}

			// Password changing requires a username, and a new password to set
			else if (subcmd == "password" && args.Parameters.Count == 3)
			{
				var account = new UserAccount();
				account.Name = args.Parameters[1];

				try
				{
					UserAccountManager.SetUserAccountPassword(account, args.Parameters[2]);
					ServerBase.Log.ConsoleInfo(GetString("{0} changed the password for account {1}", args.Player.Name, account.Name));
					args.Player.SendSuccessMessage(GetString("Password change succeeded for {0}.", account.Name));
				}
				catch (UserAccountManager.UserAccountNotExistException)
				{
					args.Player.SendErrorMessage(GetString("Account {0} does not exist! Therefore, the password cannot be changed.", account.Name));
				}
				catch (UserAccountManager.UserAccountManagerException e)
				{
					args.Player.SendErrorMessage(GetString("Password change attempt for {0} failed for an unknown reason. Check the server console for more details.", account.Name));
					ServerBase.Log.ConsoleError(e.ToString());
				}
				catch (ArgumentOutOfRangeException)
				{
					args.Player.SendErrorMessage(GetString("Password must be greater than or equal to {0} characters.", ServerBase.Config.Settings.MinimumPasswordLength));
				}
			}
			// Group changing requires a username or IP address, and a new group to set
			else if (subcmd == "group" && args.Parameters.Count == 3)
			{
				var account = new UserAccount();
				account.Name = args.Parameters[1];

				try
				{
					UserAccountManager.SetUserGroup(account, args.Parameters[2]);
					ServerBase.Log.ConsoleInfo(GetString("{0} changed account {1} to group {2}.", args.Player.Name, account.Name, args.Parameters[2]));
					args.Player.SendSuccessMessage(GetString("Account {0} has been changed to group {1}.", account.Name, args.Parameters[2]));

					//send message to player with matching account name
					var player = ServerBase.Players.FirstOrDefault(p => p != null && p.Account?.Name == account.Name);
					if (player != null && !args.Silent)
						player.SendSuccessMessage(GetString($"{args.Player.Name} has changed your group to {args.Parameters[2]}."));
				}
				catch (UserAccountManager.GroupNotExistsException)
				{
					args.Player.SendErrorMessage(GetString("That group does not exist."));
				}
				catch (UserAccountManager.UserAccountNotExistException)
				{
					args.Player.SendErrorMessage(GetString($"User {account.Name} does not exist."));
				}
				catch (UserAccountManager.UserAccountManagerException e)
				{
					args.Player.SendErrorMessage(GetString($"User {account.Name} could not be added. Check console for details."));
					ServerBase.Log.ConsoleError(e.ToString());
				}
			}
			else if (subcmd == "help")
			{
				args.Player.SendInfoMessage(GetString("User management command help:"));
				args.Player.SendInfoMessage(GetString("{0}user add username password group   -- Adds a specified user", Commands.Specifier));
				args.Player.SendInfoMessage(GetString("{0}user del username                  -- Removes a specified user", Commands.Specifier));
				args.Player.SendInfoMessage(GetString("{0}user password username newpassword -- Changes a user's password", Commands.Specifier));
				args.Player.SendInfoMessage(GetString("{0}user group username newgroup       -- Changes a user's group", Commands.Specifier));
			}
			else
			{
				args.Player.SendErrorMessage(GetString("Invalid user syntax. Try {0}user help.", Commands.Specifier));
			}
		}
	}
}

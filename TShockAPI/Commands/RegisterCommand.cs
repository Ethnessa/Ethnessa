using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.Database.Models;
using TShockAPI.Database;
using TShockAPI.ServerCommands;
using Microsoft.Xna.Framework;

namespace TShockAPI.ServerCommands
{
	public class RegisterCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string>() { "register" };
		public override CommandDelegate CommandDelegate { get; set; } = RegisterUser;
		public override List<string> Permissions { get; protected set; } = new() { TShockAPI.Permissions.canregister };
		public override bool AllowServer { get; set; } = false;
		public override bool DoLog { get; set; } = false;
		private static void RegisterUser(CommandArgs args)
		{
			try
			{
				var account = new UserAccount();
				string echoPassword = "";
				if (args.Parameters.Count == 1)
				{
					account.Name = args.Player.Name;
					echoPassword = args.Parameters[0];
					try
					{
						account.CreateBCryptHash(args.Parameters[0]);
					}
					catch (ArgumentOutOfRangeException)
					{
						args.Player.SendErrorMessage(GetString("Password must be greater than or equal to {0} characters.", TShock.Config.Settings.MinimumPasswordLength));
						return;
					}
				}
				else if (args.Parameters.Count == 2 && TShock.Config.Settings.AllowRegisterAnyUsername)
				{
					account.Name = args.Parameters[0];
					echoPassword = args.Parameters[1];
					try
					{
						account.CreateBCryptHash(args.Parameters[1]);
					}
					catch (ArgumentOutOfRangeException)
					{
						args.Player.SendErrorMessage(GetString("Password must be greater than or equal to {0} characters.", TShock.Config.Settings.MinimumPasswordLength));
						return;
					}
				}
				else
				{
					args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}register <password>.", Commands.Specifier));
					return;
				}

				account.Group = TShock.Config.Settings.DefaultRegistrationGroupName; // FIXME -- we should get this from the Database. --Why?
				account.UUID = args.Player.UUID;

				if (UserAccountManager.GetUserAccountByName(account.Name) == null && account.Name != ServerConsolePlayer.AccountName) // Cheap way of checking for existance of a user
				{
					args.Player.SendSuccessMessage(GetString("Your account, \"{0}\", has been registered.", account.Name));
					args.Player.SendSuccessMessage(GetString("Your password is {0}.", echoPassword));

					if (!TShock.Config.Settings.DisableUUIDLogin)
						args.Player.SendMessage(GetString($"Type {Commands.Specifier}login to log-in to your account using your UUID."), Color.White);

					if (TShock.Config.Settings.AllowLoginAnyUsername)
						args.Player.SendMessage(GetString($"Type {Commands.Specifier}login \"{account.Name.Color(Utils.GreenHighlight)}\" {echoPassword.Color(Utils.BoldHighlight)} to log-in to your account."), Color.White);
					else
						args.Player.SendMessage(GetString($"Type {Commands.Specifier}login {echoPassword.Color(Utils.BoldHighlight)} to log-in to your account."), Color.White);

					UserAccountManager.AddUserAccount(account);
					TShock.Log.ConsoleInfo(GetString("{0} registered an account: \"{1}\".", args.Player.Name, account.Name));
				}
				else
				{
					args.Player.SendErrorMessage(GetString("Sorry, {0} was already taken by another person.", account.Name));
					args.Player.SendErrorMessage(GetString("Please try a different username."));
					TShock.Log.ConsoleInfo(GetString("{0} attempted to register for the account {1} but it was already taken.", args.Player.Name, account.Name));
				}
			}
			catch (UserAccountManager.UserAccountManagerException ex)
			{
				args.Player.SendErrorMessage(GetString("Sorry, an error occurred: {0}.", ex.Message));
				TShock.Log.ConsoleError(GetString("RegisterUser returned an error: {0}.", ex));
			}
		}
	}
}

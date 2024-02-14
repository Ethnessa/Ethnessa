using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Database;
using EthnessaAPI.ServerCommands;

namespace EthnessaAPI.ServerCommands
{
	public class ChangePasswordCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string>() { "password" };
		public override CommandDelegate CommandDelegate { get; set; } = ChangePassword;
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.canchangepassword };
		public override bool AllowServer { get; set; } = false;
		public override bool DoLog { get; set; } = false;

		private static void ChangePassword(CommandArgs args)
		{
			try
			{
				if (args.Player.IsLoggedIn && args.Parameters.Count == 2)
				{
					string password = args.Parameters[0];
					if (args.Player.Account.VerifyPassword(password))
					{
						try
						{
							args.Player.SendSuccessMessage(GetString("You have successfully changed your password."));
							UserAccountManager.SetUserAccountPassword(args.Player.Account, args.Parameters[1]); // SetUserPassword will hash it for you.
							ServerBase.Log.ConsoleInfo(GetString("{0} ({1}) changed the password for account {2}.", args.Player.IP, args.Player.Name, args.Player.Account.Name));
						}
						catch (ArgumentOutOfRangeException)
						{
							args.Player.SendErrorMessage(GetString("Password must be greater than or equal to {0} characters.", ServerBase.Config.Settings.MinimumPasswordLength));
						}
					}
					else
					{
						args.Player.SendErrorMessage(GetString("You failed to change your password."));
						ServerBase.Log.ConsoleInfo(GetString("{0} ({1}) failed to change the password for account {2}.", args.Player.IP, args.Player.Name, args.Player.Account.Name));
					}
				}
				else
				{
					args.Player.SendErrorMessage(GetString("Not logged in or Invalid syntax. Proper syntax: {0}password <oldpassword> <newpassword>.", Commands.Specifier));
				}
			}
			catch (UserAccountManager.UserAccountManagerException ex)
			{
				args.Player.SendErrorMessage(GetString("Sorry, an error occurred: {0}.", ex.Message));
				ServerBase.Log.ConsoleError(GetString("ChangePassword returned an error: {0}.", ex));
			}
		}

	}
}

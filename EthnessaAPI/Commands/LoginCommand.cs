using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Database;
using EthnessaAPI.Hooks;
using Terraria;

namespace EthnessaAPI.ServerCommands
{
	public class LoginCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "login" };
		public override CommandDelegate CommandDelegate { get; set; } = AttemptLogin;
		public override List<string> Permissions { get; protected set; } = new() {EthnessaAPI.Permissions.canlogin };
		private static void AttemptLogin(CommandArgs args)
		{
			if (args.Player.LoginAttempts > ServerBase.Config.Settings.MaximumLoginAttempts && (ServerBase.Config.Settings.MaximumLoginAttempts != -1))
			{
				ServerBase.Log.Warn(GetString("{0} ({1}) had {2} or more invalid login attempts and was kicked automatically.",
					args.Player.IP, args.Player.Name, ServerBase.Config.Settings.MaximumLoginAttempts));
				args.Player.Kick(GetString("Too many invalid login attempts."));
				return;
			}

			if (args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage(GetString("You are already logged in, and cannot login again."));
				return;
			}

			// We need to emulate the checks done in Player.TrySwitchingLoadout, because otherwise the server is not allowed to sync the
			// loadout index to the player, causing catastrophic desync.
			// The player must not be dead, using an item, or CC'd to switch loadouts.

			// Note that we only check for CC'd players when SSC is enabled, as that is only where it can cause issues,
			// and the RequireLogin config option (without SSC) will disable player's until they login, creating a vicious cycle.

			// FIXME: There is always the chance that in-between the time we check these requirements on the server, and the loadout sync
			//		  packet reaches the client, that the client state has changed, causing the loadout sync to be rejected, even though
			//		  we expected it to succeed.

			if (args.TPlayer.dead)
			{
				args.Player.SendErrorMessage(GetString("You cannot login whilst dead."));
				return;
			}

			// FIXME: This check is not correct -- even though we reject PlayerAnimation whilst disabled, we don't re-sync it to the client,
			//		  meaning these will still be set on the client, and they WILL reject the loadout sync.
			if (args.TPlayer.itemTime > 0 || args.TPlayer.itemAnimation > 0)
			{
				args.Player.SendErrorMessage(GetString("You cannot login whilst using an item."));
				return;
			}

			if (args.TPlayer.CCed && Main.ServerSideCharacter)
			{
				args.Player.SendErrorMessage(GetString("You cannot login whilst crowd controlled."));
				return;
			}

			var account = UserAccountManager.GetUserAccountByName(args.Player.Name);
			if (account is null)
			{
				args.Player.SendErrorMessage("You do not have an account to login to.");
				return;
			}

			string password = "";
			bool usingUUID = false;
			if (args.Parameters.Count == 0 && !ServerBase.Config.Settings.DisableUUIDLogin)
			{
				if (PlayerHooks.OnPlayerPreLogin(args.Player, args.Player.Name, ""))
					return;
				usingUUID = true;
			}
			else if (args.Parameters.Count == 1)
			{
				if (PlayerHooks.OnPlayerPreLogin(args.Player, args.Player.Name, args.Parameters[0]))
					return;
				password = args.Parameters[0];
			}
			else if (args.Parameters.Count == 2 && ServerBase.Config.Settings.AllowLoginAnyUsername)
			{
				if (String.IsNullOrEmpty(args.Parameters[0]))
				{
					args.Player.SendErrorMessage(GetString("Bad login attempt."));
					return;
				}

				if (PlayerHooks.OnPlayerPreLogin(args.Player, args.Parameters[0], args.Parameters[1]))
					return;

				account = UserAccountManager.GetUserAccountByName(args.Parameters[0]);
				password = args.Parameters[1];
			}
			else
			{
				if (!ServerBase.Config.Settings.DisableUUIDLogin)
					args.Player.SendMessage(GetString($"{Commands.Specifier}login - Authenticates you using your UUID and character name."), Color.White);

				if (ServerBase.Config.Settings.AllowLoginAnyUsername)
					args.Player.SendMessage(GetString($"{Commands.Specifier}login <username> <password> - Authenticates you using your username and password."), Color.White);
				else
					args.Player.SendMessage(GetString($"{Commands.Specifier}login <password> - Authenticates you using your password and character name."), Color.White);

				args.Player.SendWarningMessage(GetString("If you forgot your password, contact the administrator for help."));
				return;
			}
			try
			{
				if (account is null)
				{
					args.Player.SendErrorMessage(GetString("A user account by that name does not exist."));
				}
				else if (account.VerifyPassword(password) ||
						(usingUUID && account.UUID == args.Player.UUID && !ServerBase.Config.Settings.DisableUUIDLogin &&
						!String.IsNullOrWhiteSpace(args.Player.UUID)))
				{
					var group = account.Group;

					if (!(GroupManager.AssertGroupValid(args.Player, group, false)))
					{
						args.Player.SendErrorMessage(GetString("Login attempt failed - see the message above."));
						return;
					}

					args.Player.PlayerData = CharacterManager.GetPlayerData(account.AccountId);

					args.Player.Account = account;
					args.Player.IsLoggedIn = true;
					args.Player.IsDisabledForSSC = false;

					if (Main.ServerSideCharacter)
					{
						if (args.Player.HasPermission(EthnessaAPI.Permissions.bypassssc))
						{
							args.Player.PlayerData.CopyCharacter(args.Player);
							CharacterManager.InsertPlayerData(args.Player);
						}
						args.Player.PlayerData.RestoreCharacter(args.Player);
					}
					args.Player.LoginFailsBySsi = false;

					if (args.Player.HasPermission(EthnessaAPI.Permissions.ignorestackhackdetection))
						args.Player.IsDisabledForStackDetection = false;

					if (args.Player.HasPermission(EthnessaAPI.Permissions.usebanneditem))
						args.Player.IsDisabledForBannedWearable = false;

					args.Player.SendSuccessMessage(GetString("Authenticated as {0} successfully.", account.Name));

					ServerBase.Log.ConsoleInfo(GetString("{0} authenticated successfully as user: {1}.", args.Player.Name, account.Name));
					if ((args.Player.LoginHarassed) && (ServerBase.Config.Settings.RememberLeavePos))
					{
						if (RememberedPosManager.GetLeavePos(args.Player.Account.AccountId) != Vector2.Zero)
						{
							var pos = RememberedPosManager.GetLeavePos(args.Player.Account.AccountId);
							if (pos is null)
								return;
							args.Player.Teleport((int)pos.Value.X * 16, (int)pos.Value.Y * 16);
						}
						args.Player.LoginHarassed = false;

					}
					UserAccountManager.SetUserAccountUUID(account, args.Player.UUID);

					Hooks.PlayerHooks.OnPlayerPostLogin(args.Player);
				}
				else
				{
					if (usingUUID && !ServerBase.Config.Settings.DisableUUIDLogin)
					{
						args.Player.SendErrorMessage(GetString("UUID does not match this character."));
					}
					else
					{
						args.Player.SendErrorMessage(GetString("Invalid password."));
					}
					ServerBase.Log.Warn(GetString("{0} failed to authenticate as user: {1}.", args.Player.IP, account.Name));
					args.Player.LoginAttempts++;
				}
			}
			catch (Exception ex)
			{
				args.Player.SendErrorMessage(GetString("There was an error processing your login or authentication related request."));
				ServerBase.Log.Error(ex.ToString());
			}
		}

	}
}

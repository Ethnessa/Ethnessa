using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using EthnessaAPI.Database;
using EthnessaAPI.Models;

namespace EthnessaAPI.ServerCommands
{
	/// <summary>
	/// This command is used to view account information. Stolen from old TShock API.
	/// </summary>
	public class AccountInfoCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string>() { "accountinfo", "ai" };
		public override CommandDelegate CommandDelegate { get; set; } = ViewAccountInfo;
		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.checkaccountinfo };
		private static void ViewAccountInfo(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}accountinfo <username>.", Commands.Specifier));
				return;
			}

			string username = String.Join(" ", args.Parameters);
			if (!string.IsNullOrWhiteSpace(username))
			{
				var account = UserAccountManager.GetUserAccountByName(username);
				if (account != null)
				{
					DateTime LastSeen;
					string Timezone = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours.ToString("+#;-#");


					LastSeen = account.LastAccessed;
					args.Player.SendSuccessMessage(GetString("{0}'s last login occurred {1} {2} UTC{3}.", account.Name, LastSeen.ToShortDateString(),
						LastSeen.ToShortTimeString(), Timezone));


					if (args.Player.Group.HasPermission(EthnessaAPI.Permissions.advaccountinfo))
					{
						List<string> KnownIps = JsonConvert.DeserializeObject<List<string>>(account.KnownIps?.ToString() ?? string.Empty);
						string ip = KnownIps?[KnownIps.Count - 1] ?? GetString("N/A");
						DateTime Registered = account.Registered;

						args.Player.SendSuccessMessage(GetString("{0}'s group is {1}.", account.Name, account.Group));
						args.Player.SendSuccessMessage(GetString("{0}'s last known IP is {1}.", account.Name, ip));
						args.Player.SendSuccessMessage(GetString("{0}'s register date is {1} {2} UTC{3}.", account.Name, Registered.ToShortDateString(), Registered.ToShortTimeString(), Timezone));
					}
				}
				else
					args.Player.SendErrorMessage(GetString("User {0} does not exist.", username));
			}
			else args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}accountinfo <username>.", Commands.Specifier));
		}

	}
}

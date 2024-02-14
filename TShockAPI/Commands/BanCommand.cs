using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TShockAPI.Database;

namespace TShockAPI.ServerCommands;

public class BanCommand : Command
{
	public override List<string> Names { get; protected set; } = new(){ "ban" };
	public override CommandDelegate CommandDelegate { get; set; } = Ban;
	public override List<string> Permissions { get; protected set; } = new() { TShockAPI.Permissions.ban };

	private static void Ban(CommandArgs args)
	{
		var player = args.Player;
		var type = args.Parameters.ElementAtOrDefault(0);

		// subcommand guard
		if (type is null)
		{
			// TODO: add message
			return;
		}

		var target = args.Parameters.ElementAtOrDefault(1);
		var reason = args.Parameters.ElementAtOrDefault(2) ?? "Misbehaviour.";
		var expiryInput = args.Parameters.ElementAtOrDefault(3) ?? "-1"; // perma ban by default
		DateTime expiry;

		if (expiryInput == "-1")
		{
			expiry = DateTime.MaxValue;
		}
		else
		{
			var timespan = ServerBase.Utils.ParseDuration(expiryInput);
			if(timespan is null)
			{
				player.SendErrorMessage("Invalid duration format. Please follow the format: 1d2h3m4s");
				return;
			}

			expiry = DateTime.Now + timespan.Value;
		}

		BanType banType;
		switch (type.ToLower())
		{
			case "ip":
			{
				if (target is null)
				{
					player.SendErrorMessage("Invalid syntax! Proper syntax: /ban ip <ip address> (reason) (time)");
					return;
				}

				bool success = IPAddress.TryParse(target, out var ip);

				if (success is false || ip is null)
				{
					player.SendErrorMessage("Invalid IP address format. It should look like this: 127.0.0.1");
					return;
				}

				target = ip.ToString();

				banType = BanType.IpAddress;
				break;
			}
			case "player":
			case "account":
			{
				if (target is null)
				{
					player.SendErrorMessage($"Invalid syntax! Proper syntax: /ban {type} <name> (reason) (time)");
					return;
				}

				var account = UserAccountManager.GetUserAccountByName(target);
				banType = BanType.AccountName;

				if(account is null)
				{
					var foundPlayers = ServerPlayer.GetByNameOrId(target);
					if(foundPlayers.Any() is false)
					{
						player.SendErrorMessage("No account or player found with that name.");
						return;
					}

					var targetPlayer = foundPlayers.First();

					if (targetPlayer.IsLoggedIn)
					{
						target = targetPlayer.Account?.Name;
					}
					else
					{
						banType = BanType.Uuid;
						target = targetPlayer.UUID;
					}
				}

				break;
			}
			case "uuid":
			{
				if (target is null)
				{
					player.SendErrorMessage("Invalid syntax! Proper syntax: /ban uuid <uuid> (reason) (time)");
					return;
				}

				banType = BanType.Uuid;
				break;
			}
			default:
			{
				player.SendErrorMessage("Invalid syntax! Proper syntax: /ban <ip|player|uuid> <value> (reason) (time)");
				return;
			}
		}

		try
		{
			var ban = BanManager.CreateBan(banType,
				target,
				reason,
				player.Account?.Name,
				DateTime.Now,
				expiry);
			player.SendSuccessMessage($"The {(type)} ({target}) has been banned. Reason: {reason} Expiry: {expiry}");
		}
		catch (Exception ex)
		{
			player.SendErrorMessage("An error occurred while trying to ban the player. If you are an admin, please check the logs for more information.");
			ServerBase.Log.ConsoleError(ex.ToString());
		}

	}
}

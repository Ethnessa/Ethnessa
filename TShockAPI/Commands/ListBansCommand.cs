using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.Database;
using TShockAPI.ServerCommands;

namespace TShockAPI.ServerCommands
{
	public class ListBansCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "listbans", "lb", "banlist", "bl"};
		public override List<string> Permissions { get; protected set; } = new() { TShockAPI.Permissions.ban };
		public override CommandDelegate CommandDelegate { get; set; } = ListBans;

		private static void ListBans(CommandArgs args)
		{
			var pageInput = args.Parameters.ElementAtOrDefault(0);
			int page = 1;
			int perPageCount = 6;

			if(pageInput is not null)
			{
				page = int.TryParse(pageInput, out int parsedPage) ? parsedPage : 1;
			}

			var bans = BanManager.GetPaginatedBans(page, perPageCount);
			var maxPages = (int)Math.Ceiling((double)BanManager.CountBans() / perPageCount);

			if(bans.Any() is false)
			{
				args.Player.SendErrorMessage("No bans found.");
				return;
			}

			args.Player.SendInfoMessage($"Bans: Page {page}/{maxPages}");
			foreach(var ban in bans)
			{
				// based on what is null, retrieve either the name, ip, or uuid
				var identifier = ban.AccountName ?? ban.IpAddress ?? ban.Uuid;
				args.Player.SendInfoMessage($"ID: {ban.BanId} - {identifier} - {ban.Reason} - {ban.GetPrettyExpirationString()}");
			}

			if(maxPages > 1)
			{
				args.Player.SendInfoMessage($"Use /listbans <page> to view more bans.");
			}

		}
	}
}

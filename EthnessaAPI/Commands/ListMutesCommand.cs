using System;
using System.Collections.Generic;
using System.Linq;
using EthnessaAPI.Database;

namespace EthnessaAPI.ServerCommands
{
	internal class ListMutesCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "lm", "listmutes", "mutes", "mutelist", "ml" };

		public override List<string> Permissions { get; protected set; } = new() { EthnessaAPI.Permissions.mute };

		public override CommandDelegate CommandDelegate { get; set; } = ListMutes;

		private static void ListMutes(CommandArgs args)
		{
			var input = args.Parameters.ElementAtOrDefault(0);

			var page = 1;
			var perPageCount = 6;

			if (input is not null)
			{
				page = int.TryParse(input, out var parsedPage) ? parsedPage : 1;
			}

			var mutes = MuteManager.GetPaginatedMutes(page, perPageCount);
			var maxPages = (int)Math.Ceiling((double)MuteManager.CountMutes() / perPageCount);

			if (mutes.Any() is false)
			{
				args.Player.SendErrorMessage("No mutes found.");
				return;
			}

			args.Player.SendInfoMessage($"Mutes: Page {page}/{maxPages}");
			foreach (var mute in mutes)
			{
				var identifier = mute.AccountName ?? mute.IpAddress ?? mute.Uuid;
				args.Player.SendInfoMessage($"ID: {mute.Id} - {identifier} - Expires: {mute.ExpiryTime}");
			}

			if (maxPages > 1)
			{
				args.Player.SendInfoMessage($"Use /listmutes <page> to view more mutes.");
			}

		}
	}
}

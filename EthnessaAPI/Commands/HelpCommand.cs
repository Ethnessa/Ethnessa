using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EthnessaAPI.Models;

namespace EthnessaAPI.ServerCommands
{
	public class HelpCommand : Command
	{
		public override List<string> Names { get; protected set; } = new() { "help" };
		public override CommandDelegate CommandDelegate { get; set; } = Help;
		public override List<string> Permissions { get; protected set; } = new() { };

		private static void Help(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendErrorMessage(GetString("Invalid syntax. Proper syntax: {0}help <command/page>", Commands.Specifier));
				return;
			}

			int pageNumber;
			if (args.Parameters.Count == 0 || int.TryParse(args.Parameters[0], out pageNumber))
			{
				if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
				{
					return;
				}

				IEnumerable<string> cmdNames = Commands.ServerCommands.Where(cmd => cmd.CanRun(args.Player)).Select(cmd => Commands.Specifier + cmd.Name);

				PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cmdNames),
					new PaginationTools.Settings
					{
						HeaderFormat = GetString("Commands ({{0}}/{{1}}):"),
						FooterFormat = GetString("Type {0}help {{0}} for more.", Commands.Specifier)
					});
			}
			else
			{
				string commandName = args.Parameters[0].ToLower();
				if (commandName.StartsWith(Commands.Specifier))
				{
					commandName = commandName.Substring(1);
				}

				Command command = Commands.ServerCommands.Find(c => c.Names.Contains(commandName));
				if (command == null)
				{
					args.Player.SendErrorMessage(GetString("Invalid command."));
					return;
				}
				if (!command.CanRun(args.Player))
				{
					args.Player.SendErrorMessage(GetString("You do not have access to this command."));
					return;
				}

				args.Player.SendSuccessMessage(GetString("{0}{1} help: ", Commands.Specifier, command.Name));
				if (command.HelpDesc == null)
				{
					args.Player.SendInfoMessage(command.HelpText);
					return;
				}
				foreach (string line in command.HelpDesc)
				{
					args.Player.SendInfoMessage(line);
				}
			}
		}

	}
}

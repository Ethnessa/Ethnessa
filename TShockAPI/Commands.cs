using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI.Database;
using TShockAPI.ServerCommands;

namespace TShockAPI
{
	public delegate void CommandDelegate(CommandArgs args);

	// TODO: Make each command a class, and have a CommandManager class that handles all the commands.
	// We want to make this disaster a bit more readable and easy to maintain in the future.
	public class CommandArgs : EventArgs
	{
		public string Message { get; private set; }
		public ServerPlayer Player { get; private set; }
		public bool Silent { get; private set; }

		/// <summary>
		/// Parameters passed to the argument. Does not include the command name.
		/// IE '/kick "jerk face"' will only have 1 argument
		/// </summary>
		public List<string> Parameters { get; private set; }

		public Player TPlayer => Player.TPlayer;

		public CommandArgs(string message, ServerPlayer ply, List<string> args)
		{
			Message = message;
			Player = ply;
			Parameters = args;
			Silent = false;
		}

		public CommandArgs(string message, bool silent, ServerPlayer ply, List<string> args)
		{
			Message = message;
			Player = ply;
			Parameters = args;
			Silent = silent;
		}
	}

	public static class Commands
	{
		// TODO: Merge into one List
		public static List<Command> ChatCommands = new List<Command>();
		public static List<Command> TShockCommands = new List<Command>(new List<Command>());

		/// <summary>
		/// The command specifier, defaults to "/"
		/// </summary>
		public static string Specifier => string.IsNullOrWhiteSpace(ServerBase.Config.Settings.CommandSpecifier) ? "/" : ServerBase.Config.Settings.CommandSpecifier;

		/// <summary>
		/// The silent command specifier, defaults to "."
		/// </summary>
		public static string SilentSpecifier => string.IsNullOrWhiteSpace(ServerBase.Config.Settings.CommandSilentSpecifier) ? "." : ServerBase.Config.Settings.CommandSilentSpecifier;

		private delegate void AddChatCommand(string permission, CommandDelegate command, params string[] names);

		public static void InitCommands()
		{
			List<Command> tshockCommands = new List<Command>();
			Action<Command> add = (cmd) =>
			{
				tshockCommands.Add(cmd);
				ChatCommands.Add(cmd);
			};

			// TODO: just add these to a list, instead of calling the add action for each one
			add(new UserCommand());
			add(new StopCommand());
			add(new GroupCommand());
			add(new HelpCommand());
			add(new UserInfoCommand());
			add(new LoginCommand());
			add(new LogoutCommand());
			add(new RegisterCommand());
			add(new ChangePasswordCommand());
			add(new AccountInfoCommand());
			add(new KickCommand());
			add(new ConfigCommand());
			add(new SetSpawnCommand());
			add(new SpawnCommand());
			add(new BanCommand());
			add(new UuidCommand());

			TShockCommands = new List<Command>(tshockCommands);
		}

		/// <summary>
		/// Executes a command as a player.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="text"></param>
		/// <returns>Was the player able to run the command?</returns>
		public static bool HandleCommand(ServerPlayer player, string text)
		{
			string cmdText = text.Remove(0, 1);
			string cmdPrefix = text[0].ToString();
			bool silent = cmdPrefix == SilentSpecifier;

			int index = -1;
			for (int i = 0; i < cmdText.Length; i++)
			{
				if (IsWhiteSpace(cmdText[i]))
				{
					index = i;
					break;
				}
			}
			string cmdName;
			if (index == 0) // Space after the command specifier should not be supported
			{
				player.SendErrorMessage(GetString("You entered a space after {0} instead of a command. Type {0}help for a list of valid commands.", Specifier));
				return true;
			}
			else if (index < 0)
				cmdName = cmdText.ToLower();
			else
				cmdName = cmdText.Substring(0, index).ToLower();

			List<string> args;
			if (index < 0)
				args = new List<string>();
			else
				args = ParseParameters(cmdText.Substring(index));

			IEnumerable<Command> cmds = ChatCommands.FindAll(c => c.HasAlias(cmdName));

			if (Hooks.PlayerHooks.OnPlayerCommand(player, cmdName, cmdText, args, ref cmds, cmdPrefix))
				return true;

			if (!cmds.Any())
			{
				if (player.AwaitingResponse.ContainsKey(cmdName))
				{
					Action<CommandArgs> call = player.AwaitingResponse[cmdName];
					player.AwaitingResponse.Remove(cmdName);
					call(new CommandArgs(cmdText, player, args));
					return true;
				}
				player.SendErrorMessage(GetString("Invalid command entered. Type {0}help for a list of valid commands.", Specifier));
				return true;
			}
			foreach (Command cmd in cmds)
			{
				if (!cmd.CanRun(player))
				{
					if (cmd.DoLog)
						ServerBase.Utils.SendLogs(GetString("{0} tried to execute {1}{2}.", player.Name, Specifier, cmdText), Color.PaleVioletRed, player);
					else
						ServerBase.Utils.SendLogs(GetString("{0} tried to execute (args omitted) {1}{2}.", player.Name, Specifier, cmdName), Color.PaleVioletRed, player);
					player.SendErrorMessage(GetString("You do not have access to this command."));
					if (player.HasPermission(Permissions.su))
					{
						player.SendInfoMessage(GetString("You can use '{0}sudo {0}{1}' to override this check.", Specifier, cmdText));
					}
				}
				else if (!cmd.AllowServer && !player.RealPlayer)
				{
					player.SendErrorMessage(GetString("You must use this command in-game."));
				}
				else
				{
					if (cmd.DoLog)
						ServerBase.Utils.SendLogs(GetString("{0} executed: {1}{2}.", player.Name, silent ? SilentSpecifier : Specifier, cmdText), Color.PaleVioletRed, player);
					else
						ServerBase.Utils.SendLogs(GetString("{0} executed (args omitted): {1}{2}.", player.Name, silent ? SilentSpecifier : Specifier, cmdName), Color.PaleVioletRed, player);
					cmd.Run(cmdText, silent, player, args);
				}
			}
			return true;
		}

		/// <summary>
		/// Parses a string of parameters into a list. Handles quotes.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private static List<string> ParseParameters(string str)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			bool instr = false;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (c == '\\' && ++i < str.Length)
				{
					if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
						sb.Append('\\');
					sb.Append(str[i]);
				}
				else if (c == '"')
				{
					instr = !instr;
					if (!instr)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
					else if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else if (IsWhiteSpace(c) && !instr)
				{
					if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else
					sb.Append(c);
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}

		private static bool IsWhiteSpace(char c)
		{
			return c == ' ' || c == '\t' || c == '\n';
		}

	}
}

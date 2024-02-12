using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI.ServerCommands;

namespace TShockAPI.ServerCommands
{
	/// <summary>
	/// Change config values from in-game, via command
	/// </summary>
	public class ConfigCommand : Command
	{
		public override List<string> Names { get; protected set; } = new List<string> { "config" };
		public override CommandDelegate CommandDelegate { get; set; } = Execute;
		public override List<string> Permissions { get; protected set; } = new() { TShockAPI.Permissions.config };
		private static void Execute(CommandArgs args)
		{
			var subcmd = args.Parameters.ElementAtOrDefault(0);

			switch (subcmd)
			{
				case "value":
				case "setvalue":
				case "set":
					{
						var key = args.Parameters.ElementAtOrDefault(1);
						var value = args.Parameters.ElementAtOrDefault(2);
						if (key == null || value == null)
						{
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /config set <key> <value>");
							return;
						}

						// get field from config, case in-sensitive
						var setting = ServerBase.Config.GetType().GetField(key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
						if (setting == null)
						{
							args.Player.SendErrorMessage("Invalid key!");
							return;
						}

						// if valid value, set the setting and write to file
						if (setting.FieldType == typeof(string))
						{
							setting.SetValue(ServerBase.Config, value);
							ServerBase.Config.Write(FileTools.ConfigPath);
							args.Player.SendSuccessMessage("Set {0} to {1}", key, value);
						}
						else if (setting.FieldType == typeof(int))
						{
							if (int.TryParse(value, out int result))
							{
								setting.SetValue(ServerBase.Config, result);
								ServerBase.Config.Write(FileTools.ConfigPath);
								args.Player.SendSuccessMessage("Set {0} to {1}", key, value);
							}
							else
							{
								args.Player.SendErrorMessage("Invalid value!");
							}
						}
						else if (setting.FieldType == typeof(bool))
						{
							if (bool.TryParse(value, out bool result))
							{
								setting.SetValue(ServerBase.Config, result);
								ServerBase.Config.Write(FileTools.ConfigPath);
								args.Player.SendSuccessMessage("Set {0} to {1}", key, value);
							}
							else
							{
								args.Player.SendErrorMessage("Invalid value!");
							}
						}
						else
						{
							args.Player.SendErrorMessage("Invalid value!");
						}
						break;
					}
					case "reload":
					{
						ServerBase.Utils.Reload();
						Hooks.GeneralHooks.OnReloadEvent(args.Player);
						args.Player.SendSuccessMessage("Reloaded TShock configuration.");
						break;
					}
					default:
					{
						// send help command
						args.Player.SendInfoMessage("Proper syntax: /config <setvalue|reload>");
						break;
					}
			}

		}
	}
}

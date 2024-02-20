using System;
using System.Collections.Generic;
using Terraria;

namespace EthnessaAPI.Models;

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

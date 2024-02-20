namespace EthnessaAPI.Models;

public class CommandAlias
{
	public string Alias { get; set; }
	public string Command { get; set; }

	public CommandAlias(string alias, string command)
	{
		Alias = alias;
		Command = command;
	}
}

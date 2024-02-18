namespace EthnessaAPI.Database.Models;

public class TagStatus
{
	public string Name { get; set; }
	public bool Enabled { get; set; }

	public TagStatus(string name, bool enabled)
	{
		Name = name;
		Enabled = enabled;
	}
}

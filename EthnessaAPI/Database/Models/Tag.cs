using MongoDB.Bson;

namespace EthnessaAPI.Database.Models;

public class Tag
{
	public ObjectId Id { get; set; }
	public string Name { get; set; }
	public string FormattedText { get; set; }

	public Tag(string name, string formattedText)
	{
		Name = name;
		FormattedText = formattedText;
	}
}

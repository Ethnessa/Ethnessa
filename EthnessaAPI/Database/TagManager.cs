using MongoDB.Driver;

namespace EthnessaAPI.Database;

public static class TagManager
{
	internal static string CollectionName = "tags";
	private static IMongoCollection<Models.Tag> tags = ServerBase.GlobalDatabase.GetCollection<Models.Tag>(CollectionName);

	public static Models.Tag? GetTag(string name)
	{
		return tags.Find(x => x.Name == name).FirstOrDefault();
	}

	public static bool DoesTagExist(string name)
	{
		return GetTag(name) is not null;
	}

	public static bool UpdateTag(string name, string formattedTagText)
	{
		var tag = GetTag(name);

		if (tag is null)
		{
			return false;
		}

		tag.FormattedText = formattedTagText;
		tags.ReplaceOne(x => x.Name == name, tag);
		return true;
	}

	public static bool DeleteTag(string name)
	{
		if (!DoesTagExist(name))
		{
			return false;
		}

		tags.DeleteOne(x => x.Name == name);
		return true;
	}

	public static bool CreateTag(string name, string formattedTagText)
	{
		if (DoesTagExist(name))
		{
			return false;
		}

		var tag = new Models.Tag(name, formattedTagText);
		tags.InsertOne(tag);
		return true;
	}
}

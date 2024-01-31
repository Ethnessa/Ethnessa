using NUnit.Framework;
using TShockAPI;
using TShockAPI.Database;

namespace TShockLauncher.Tests;

public class GroupTests
{
	/// <summary>
	/// This tests to ensure the group commands work.
	/// </summary>
	[TestCase]
	public void TestPermissions()
	{
		var groups = TShock.Groups = new GroupManager(TShock.DB);

		if (!groups.GroupExists("test"))
			groups.AddGroup("test", null, "test", Group.defaultChatColor);

		groups.AddPermissions("test", new() { "abc" });

		var hasperm = groups.GetGroupByName("test").Permissions.Contains("abc");
		Assert.IsTrue(hasperm);

		groups.DeletePermissions("test", new() { "abc" });

		hasperm = groups.GetGroupByName("test").Permissions.Contains("abc");
		Assert.IsFalse(hasperm);

		groups.DeleteGroup("test");

		var g = groups.GetGroupByName("test");
		Assert.IsNull(g);
	}
}


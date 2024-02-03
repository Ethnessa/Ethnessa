using NUnit.Framework;
using TShockAPI;

namespace TShockLauncher.Tests;

[TestFixture]
public class PlayerArrayAccess
{
	[TestCase]
	public void TestPlayerArrayAccess()
	{
		// Arrange
		var players = TShock.Players;

		// Act
		var result = players[0];

		Assert.That(result is not null, "The player should return as null null");
	}
}

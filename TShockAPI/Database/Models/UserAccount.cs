using System;
using System.Threading.Tasks;
using BCrypt.Net;
using MongoDB.Entities;

namespace TShockAPI.Database.Models;

/// <summary>A database user account.</summary>
public class UserAccount : Entity
{
	/// <summary>The database AccountId of the user account.</summary>
	public int AccountId { get; set; }

	/// <summary>The user's name.</summary>
	public string Name { get; set; } = "";

	/// <summary>The hashed password for the user account.</summary>
	public string Password { get; set; } = "";

	/// <summary>The user's saved Universally Unique Identifier token.</summary>
	public string UUID { get; set; } = "";

	/// <summary>The group object that the user account is a part of.</summary>
	public string Group { get; set; } = "default";

	/// <summary>The unix epoch corresponding to the registration date of the user account.</summary>
	public DateTime Registered { get; set; } = DateTime.Now;

	/// <summary>The unix epoch corresponding to the last access date of the user account.</summary>
	public DateTime LastAccessed { get; set; } = DateTime.Now;

	/// <summary>A JSON serialized list of known IP addresses for a user account.</summary>
	public string KnownIps { get; set; } = "";

	/// <summary>Constructor for the user account object, assuming you define everything yourself.</summary>
	/// <param name="name">The user's name.</param>
	/// <param name="pass">The user's password hash.</param>
	/// <param name="uuid">The user's UUID.</param>
	/// <param name="group">The user's group name.</param>
	/// <param name="registered">The unix epoch for the registration date.</param>
	/// <param name="last">The unix epoch for the last access date.</param>
	/// <param name="known">The known IPs for the user account, serialized as a JSON object</param>
	/// <returns>A completed user account object.</returns>
	public UserAccount(string name, string pass, string uuid, string group, DateTime registered, DateTime last,
		string known)
	{
		Name = name;
		Password = pass;
		UUID = uuid;
		Group = group;
		Registered = registered;
		LastAccessed = last;
		KnownIps = known;
	}

	public UserAccount(){}

	/// <summary>
	/// Verifies if a password matches the one stored in the database.
	/// If the password is stored in an unsafe hashing algorithm, it will be converted to BCrypt.
	/// If the password is stored using BCrypt, it will be re-saved if the work factor in the config
	/// is greater than the existing work factor with the new work factor.
	/// </summary>
	/// <param name="password">The password to check against the user account object.</param>
	/// <returns>bool true, if the password matched, or false, if it didn't.</returns>
	public bool VerifyPassword(string password)
	{
		try
		{
			if (BCrypt.Net.BCrypt.Verify(password, Password))
			{
				// If necessary, perform an upgrade to the highest work factor.
				UpgradePasswordWorkFactor(password);
				return true;
			}
		}
		catch (SaltParseException)
		{
			TShock.Log.ConsoleError(GetString($"Unable to verify the password hash for user {Name} ({AccountId})"));
			return false;
		}

		return false;
	}

	/// <summary>Upgrades a password to the highest work factor available in the config.</summary>
	/// <param name="password">The raw user account password (unhashed) to upgrade</param>
	protected async Task UpgradePasswordWorkFactor(string password)
	{
		// If the destination work factor is not greater, we won't upgrade it or re-hash it
		int currentWorkFactor;
		try
		{
			currentWorkFactor = Int32.Parse((Password.Split('$')[2]));
		}
		catch (FormatException)
		{
			TShock.Log.ConsoleWarn(
				GetString("Not upgrading work factor because bcrypt hash in an invalid format."));
			return;
		}

		if (currentWorkFactor < TShock.Config.Settings.BCryptWorkFactor)
		{
			try
			{
				await UserAccountManager.SetUserAccountPassword(this, password);
			}
			catch (UserAccountManager.UserAccountManagerException e)
			{
				TShock.Log.ConsoleError(e.ToString());
			}
		}
	}

	/// <summary>Creates a BCrypt hash for a user account and stores it in this object.</summary>
	/// <param name="password">The plain text password to hash</param>
	public void CreateBCryptHash(string password)
	{
		if (password.Trim().Length < Math.Max(4, TShock.Config.Settings.MinimumPasswordLength))
		{
			int minLength = TShock.Config.Settings.MinimumPasswordLength;
			throw new ArgumentOutOfRangeException("password",
				GetString($"Password must be at least {minLength} characters."));
		}

		try
		{
			Password = BCrypt.Net.BCrypt.HashPassword(password.Trim(), TShock.Config.Settings.BCryptWorkFactor);
		}
		catch (ArgumentOutOfRangeException)
		{
			TShock.Log.ConsoleError(GetString(
				"Invalid BCrypt work factor in config file! Creating new hash using default work factor."));
			Password = BCrypt.Net.BCrypt.HashPassword(password.Trim());
		}
	}

	/// <summary>Creates a BCrypt hash for a user account and stores it in this object.</summary>
	/// <param name="password">The plain text password to hash</param>
	/// <param name="workFactor">The work factor to use in generating the password hash</param>
	public void CreateBCryptHash(string password, int workFactor)
	{
		if (password.Trim().Length < Math.Max(4, TShock.Config.Settings.MinimumPasswordLength))
		{
			int minLength = TShock.Config.Settings.MinimumPasswordLength;
			throw new ArgumentOutOfRangeException("password",
				GetString($"Password must be at least {minLength} characters."));
		}

		Password = BCrypt.Net.BCrypt.HashPassword(password.Trim(), workFactor);
	}

	#region IEquatable

	/// <summary>Indicates whether the current <see cref="UserAccount"/> is equal to another <see cref="UserAccount"/>.</summary>
	/// <returns>true if the <see cref="UserAccount"/> is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
	/// <param name="other">An <see cref="UserAccount"/> to compare with this <see cref="UserAccount"/>.</param>
	public bool Equals(UserAccount other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return AccountId == other.AccountId && string.Equals(Name, other.Name);
	}

	/// <summary>Indicates whether the current <see cref="UserAccount"/> is equal to another object.</summary>
	/// <returns>true if the <see cref="UserAccount"/> is equal to the <paramref name="obj" /> parameter; otherwise, false.</returns>
	/// <param name="obj">An <see cref="object"/> to compare with this <see cref="UserAccount"/>.</param>
	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != this.GetType()) return false;
		return Equals((UserAccount)obj);
	}

	/// <summary>Serves as the hash function. </summary>
	/// <returns>A hash code for the current <see cref="UserAccount"/>.</returns>
	public override int GetHashCode()
	{
		unchecked
		{
			return (AccountId * 397) ^ (Name != null ? Name.GetHashCode() : 0);
		}
	}

	/// <summary>
	/// Compares equality of two <see cref="UserAccount"/> objects.
	/// </summary>
	/// <param name="left">Left hand of the comparison.</param>
	/// <param name="right">Right hand of the comparison.</param>
	/// <returns>true if the <see cref="UserAccount"/> objects are equal; otherwise, false.</returns>
	public static bool operator ==(UserAccount left, UserAccount right)
	{
		return Equals(left, right);
	}

	/// <summary>
	/// Compares equality of two <see cref="UserAccount"/> objects.
	/// </summary>
	/// <param name="left">Left hand of the comparison.</param>
	/// <param name="right">Right hand of the comparison.</param>
	/// <returns>true if the <see cref="UserAccount"/> objects aren't equal; otherwise, false.</returns>
	public static bool operator !=(UserAccount left, UserAccount right)
	{
		return !Equals(left, right);
	}

	#endregion

	/// <summary>
	/// Converts the UserAccount to it's string representation
	/// </summary>
	/// <returns>Returns the UserAccount string representation</returns>
	public override string ToString() => Name;
}

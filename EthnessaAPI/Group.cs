using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using EthnessaAPI.Database;

namespace EthnessaAPI
{
	/// <summary>
	/// A class used to group multiple users' permissions and settings.
	/// </summary>
	public class Group
	{
		public ObjectId Id { get; set; }
		/// <summary>
		/// Default chat color.
		/// </summary>
		public const string DefaultChatColor = "255,255,255";

		/// <summary>
		/// List of permissions available to the group.
		/// </summary>
		public virtual List<string> Permissions { get; set; } = new List<string>();

		/// <summary>
		/// List of permissions that the group is explicitly barred from.
		/// </summary>
		public virtual List<string> NegatedPermissions { get; set; } = new List<string>();

		/// <summary>
		/// The group's name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The chat prefix for this group.
		/// </summary>
		public string Prefix { get; set; }

		/// <summary>
		/// The chat suffix for this group.
		/// </summary>
		public string Suffix { get; set; }

		/// <summary>
		/// The name of the parent group, if any.
		/// </summary>
		public string? ParentGroupName { get; set; }

		/// <summary>
		/// Retrieves the parent group of this group.
		/// </summary>
		/// <returns>The parent group, or null</returns>
		public Group? GetParentGroup()
		{
			return GroupManager.GetGroupByName(ParentGroupName);
		}

		/// <summary>
		/// The chat color of the group in "R,G,B" format. Each component should be in the range 0-255.
		/// </summary>
		public string ChatColor
		{
			get => $"{R:D3},{G:D3},{B:D3}";
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value), "ChatColor cannot be null.");

				var parts = value.Split(',');
				if (parts.Length != 3)
					throw new ArgumentException("ChatColor must be in the format \"R,G,B\".", nameof(value));

				if (byte.TryParse(parts[0], out var r) && byte.TryParse(parts[1], out var g) &&
				    byte.TryParse(parts[2], out var b))
				{
					R = r;
					G = g;
					B = b;
				}
				else
				{
					throw new ArgumentException(
						"Each component of ChatColor must be a valid byte value in the range 0-255.", nameof(value));
				}
			}
		}


		/// <summary>
		/// The permissions of this group and all that it inherits from.
		/// </summary>
		public virtual List<string> GetPermissions()
		{
			var perms = new List<string>(Permissions);
			var parent = GetParentGroup();
			while (parent != null)
			{
				perms.AddRange(parent.Permissions);
				perms.RemoveAll(parent.NegatedPermissions.Contains);
				parent = parent.GetParentGroup();
			}

			return perms;
		}

		/// <summary>
		/// The group's chat color red byte.
		/// </summary>
		public byte R = 255;

		/// <summary>
		/// The group's chat color green byte.
		/// </summary>
		public byte G = 255;

		/// <summary>
		/// The group's chat color blue byte.
		/// </summary>
		public byte B = 255;

		/// <summary>
		/// The default group attributed to unregistered users.
		/// </summary>
		public static Group? DefaultGroup = null;

		/// <summary>
		/// Initializes a new instance of the group class.
		/// </summary>
		/// <param name="groupname">The name of the group.</param>
		/// <param name="parentgroup">The parent group, if any.</param>
		/// <param name="chatcolor">The chat color, in "RRR,GGG,BBB" format.</param>
		/// <param name="permissions">The list of permissions associated with this group, separated by commas.</param>
		public Group(string groupname, Group? parentgroup = null, string chatcolor = "255,255,255",
			List<string> permissions = null)
		{
			Name = groupname;
			ParentGroupName = parentgroup?.Name;
			ChatColor = chatcolor;
			Permissions = permissions;
		}

		/// <summary>
		/// Checks to see if a group has a specified permission.
		/// </summary>
		/// <param name="permission">The permission to check.</param>
		/// <returns>True if the group has that permission.</returns>
		public virtual bool HasPermission(string permission)
		{
			var perms = GetPermissions();
			return perms.Contains(permission) && !NegatedPermissions.Contains(permission);
		}

		/// <summary>
		/// Adds a permission to the list of negated permissions.
		/// </summary>
		/// <param name="permission">The permission to negate.</param>
		public void NegatePermission(string permission)
		{
			// Avoid duplicates
			if (!NegatedPermissions.Contains(permission))
			{
				NegatedPermissions.Add(permission);
				Permissions.Remove(permission); // Ensure we don't have conflicting definitions for a permissions
			}
		}

		/// <summary>
		/// Adds a permission to the list of permissions.
		/// </summary>
		/// <param name="permission">The permission to add.</param>
		public void AddPermission(string permission)
		{
			if (permission.StartsWith("!"))
			{
				NegatePermission(permission.Substring(1));
				return;
			}

			// Avoid duplicates
			if (!Permissions.Contains(permission))
			{
				Permissions.Add(permission);
				NegatedPermissions.Remove(permission); // Ensure we don't have conflicting definitions for a permissions
			}
		}

		/// <summary>
		/// Clears the permission list and sets it to the list provided,
		/// will parse "!permission" and add it to the negated permissions.
		/// </summary>
		/// <param name="permission">The new list of permissions to associate with the group.</param>
		public void SetPermission(List<string> permission)
		{
			Permissions.Clear();
			NegatedPermissions.Clear();
			permission.ForEach(AddPermission);
		}

		/// <summary>
		/// Will remove a permission from the respective list,
		/// where "!permission" will remove a negated permission.
		/// </summary>
		/// <param name="permission"></param>
		public void RemovePermission(string permission)
		{
			if (permission.StartsWith("!"))
			{
				NegatedPermissions.Remove(permission.Substring(1));
				return;
			}

			Permissions.Remove(permission);
		}

		/// <summary>
		/// Assigns all fields of this instance to another.
		/// </summary>
		/// <param name="otherGroup">The other instance.</param>
		public void AssignTo(Group otherGroup)
		{
			otherGroup.Name = Name;
			otherGroup.ParentGroupName = ParentGroupName;
			otherGroup.Prefix = Prefix;
			otherGroup.Suffix = Suffix;
			otherGroup.R = R;
			otherGroup.G = G;
			otherGroup.B = B;
			otherGroup.Permissions = Permissions;
		}

		public override string ToString()
		{
			return this.Name;
		}
	}
}

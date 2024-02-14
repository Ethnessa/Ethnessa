/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace TShockAPI.Localization
{
	/// <summary>
	/// Provides a series of methods that give English texts
	/// </summary>
	public static class EnglishLanguage
	{
		public static readonly Dictionary<int, string> ItemNames = new Dictionary<int, string>();

		public static readonly Dictionary<int, string> NpcNames = new Dictionary<int, string>();

		public static readonly Dictionary<int, string> Prefixs = new Dictionary<int, string>();

		public static readonly Dictionary<int, string> Buffs = new Dictionary<int, string>();

		internal static void Initialize()
		{
			var culture = Language.ActiveCulture;

			var skip = culture == GameCulture.FromCultureName(GameCulture.CultureName.English);

			try
			{
				if (!skip)
				{
					LanguageManager.Instance.SetLanguage(GameCulture.FromCultureName(GameCulture.CultureName.English));
				}

				for (var i = -48; i < Terraria.ID.ItemID.Count; i++)
				{
					ItemNames.Add(i, Lang.GetItemNameValue(i));
				}

				for (var i = -17; i < Terraria.ID.NPCID.Count; i++)
				{
					NpcNames.Add(i, Lang.GetNPCNameValue(i));
				}

				for (var i = 0; i < Terraria.ID.BuffID.Count; i++)
				{
					Buffs.Add(i, Lang.GetBuffName(i));
				}

				try
				{
					// Ensure the type is found before proceeding
					var prefixIdType = typeof(Main).Assembly.GetType("Terraria.AccountId.PrefixID");
					if (prefixIdType != null)
					{
						// Iterate over fields excluding "Count"
						foreach (var field in prefixIdType.GetFields().Where(f => !f.Name.Equals("Count", StringComparison.Ordinal)))
						{
							// Ensure the field is static and of type int
							if (field.IsStatic && field.FieldType == typeof(int))
							{
								var i = (int)field.GetValue(null); // Safe cast as we checked field type

								// Ensure Lang.prefix is properly initialized and index exists
								if (Lang.prefix != null && i >= 0 && i < Lang.prefix.Length && Lang.prefix[i] != null)
								{
									Prefixs.Add(i, Lang.prefix[i].Value);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					ServerBase.Log.ConsoleError($"Error processing fields: {ex.Message}");
				}

			}
			finally
			{
				if (!skip)
				{
					LanguageManager.Instance.SetLanguage(culture);
				}
			}
		}

		/// <summary>
		/// Get the english name of an item
		/// </summary>
		/// <param name="id">AccountId of the item</param>
		/// <returns>Item name in English</returns>
		public static string GetItemNameById(int id)
		{
			string itemName;
			if (ItemNames.TryGetValue(id, out itemName))
				return itemName;

			return null;
		}

		/// <summary>
		/// Get the english name of a npc
		/// </summary>
		/// <param name="id">AccountId of the npc</param>
		/// <returns>Npc name in English</returns>
		public static string GetNpcNameById(int id)
		{
			string npcName;
			if (NpcNames.TryGetValue(id, out npcName))
				return npcName;

			return null;
		}

		/// <summary>
		/// Get prefix in English
		/// </summary>
		/// <param name="id">Prefix AccountId</param>
		/// <returns>Prefix in English</returns>
		public static string GetPrefixById(int id)
		{
			string prefix;
			if (Prefixs.TryGetValue(id, out prefix))
				return prefix;

			return null;
		}

		/// <summary>
		/// Get buff name in English
		/// </summary>
		/// <param name="id">Buff AccountId</param>
		/// <returns>Buff name in English</returns>
		public static string GetBuffNameById(int id)
		{
			string buff;
			if (Buffs.TryGetValue(id, out buff))
				return buff;

			return null;
		}
	}
}

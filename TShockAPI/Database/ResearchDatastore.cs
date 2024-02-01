using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Entities;
using Terraria;
using Terraria.ID;
using TShockAPI.Database.Models;

namespace TShockAPI.Database
{
	/// <summary>
	/// This class is used as the data interface for Journey mode research.
	/// This information is maintained such that SSC characters will be properly set up with
	/// the world's current research.
	/// </summary>
	public static class ResearchDatastore
	{
		/// <summary>
		/// In-memory cache of what items have been sacrificed.
		/// The first call to GetSacrificedItems will load this with data from the database.
		/// </summary>
		private static Dictionary<int, int> _itemsSacrificed;



		/// <summary>
		/// This call will return the memory-cached list of items sacrificed.
		/// If the cache is not initialized, it will be initialized from the database.
		/// </summary>
		/// <returns></returns>
		public static async Task<Dictionary<int, int>> GetSacrificedItems()
		{
			return _itemsSacrificed ?? await ReadFromDatabase();
		}

		/// <summary>
		/// This function will return a Dictionary&lt;ItemId, AmountSacrificed&gt; representing
		/// what the progress of research on items is for this world.
		/// </summary>
		/// <returns>A dictionary of ItemID keys and Amount Sacrificed values.</returns>
		private static async Task<Dictionary<int, int>> ReadFromDatabase()
		{
			Dictionary<int, int> sacrificedItems = new Dictionary<int, int>();
			var fromDatabase = await DB.Find<SacrificedItem>()
				.Match(x => x.WorldId == Main.worldID)
				.ExecuteAsync();

			foreach(var item in fromDatabase)
			{
				sacrificedItems[item.ItemId] = item.AmountSacrified;
			}

			return sacrificedItems;
		}

		/// <summary>
		/// This method will sacrifice an amount of an item for research.
		/// </summary>
		/// <param name="itemId">The net ItemId that is being researched.</param>
		/// <param name="amount">The amount of items being sacrificed.</param>
		/// <param name="player">The player who sacrificed the item for research.</param>
		/// <returns>The cumulative total sacrifices for this item.</returns>
		public static async Task<int> SacrificeItem(int itemId, int amount, TSPlayer player)
		{
			var itemsSacrificed = await GetSacrificedItems();
			if (!(itemsSacrificed.ContainsKey(itemId)))
				itemsSacrificed[itemId] = 0;

			try
			{
				SacrificedItem item = new SacrificedItem(Main.worldID, player.Account.ID, itemId, amount);
				await item.SaveAsync();
				itemsSacrificed[itemId] += amount;

			}catch(Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return itemsSacrificed[itemId];
		}
	}
}

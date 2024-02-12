using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
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
		private static IMongoCollection<SacrificedItem> sacrificedItems => TShock.GlobalDatabase.GetCollection<SacrificedItem>("sacrificeditems");
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
		public static Dictionary<int, int> GetSacrificedItems()
		{
			return _itemsSacrificed ?? ReadFromDatabase();
		}

		/// <summary>
		/// This function will return a Dictionary&lt;ItemId, AmountSacrificed&gt; representing
		/// what the progress of research on items is for this world.
		/// </summary>
		/// <returns>A dictionary of ItemID keys and Amount Sacrificed values.</returns>
		private static Dictionary<int, int> ReadFromDatabase()
		{
			Dictionary<int, int> sacItems = new Dictionary<int, int>();
			var fromDatabase = sacrificedItems.Find<SacrificedItem>(x => x.WorldId == Main.worldID)
				.ToList();

			foreach(var item in fromDatabase)
			{
				sacItems[item.ItemId] = item.AmountSacrified;
			}

			return sacItems;
		}

		/// <summary>
		/// This method will sacrifice an amount of an item for research.
		/// </summary>
		/// <param name="itemId">The net ItemId that is being researched.</param>
		/// <param name="amount">The amount of items being sacrificed.</param>
		/// <param name="player">The player who sacrificed the item for research.</param>
		/// <returns>The cumulative total sacrifices for this item.</returns>
		public static int SacrificeItem(int itemId, int amount, ServerPlayer player)
		{
			var itemsSacrificed = GetSacrificedItems();
			if (!(itemsSacrificed.ContainsKey(itemId)))
				itemsSacrificed[itemId] = 0;

			try
			{
				SacrificedItem item = new SacrificedItem(Main.worldID, player.Account.AccountId, itemId, amount);
				sacrificedItems.InsertOne(item);
				itemsSacrificed[itemId] += amount;

			}catch(Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return itemsSacrificed[itemId];
		}
	}
}

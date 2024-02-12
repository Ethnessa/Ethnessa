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
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using MongoDB.Bson;

namespace TShockAPI.Database
{
	public static class WarpManager
	{
		private static IMongoCollection<Warp> warps => ServerBase.GlobalDatabase.GetCollection<Warp>("warps");
		/// <summary>
		/// Adds a warp.
		/// </summary>
		/// <param name="x">The X position.</param>
		/// <param name="y">The Y position.</param>
		/// <param name="name">The name.</param>
		/// <returns>Whether the operation succeeded.</returns>
		public static bool Add(int x, int y, string name)
		{
			try
			{
				Warp warp = new Warp(new Point(x,y), name);
				warps.InsertOne(warp);
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Removes a warp.
		/// </summary>
		/// <param name="warpName">The warp name.</param>
		/// <returns>Whether the operation succeeded.</returns>
		public static bool Remove(string warpName)
		{
			try
			{
				warps.FindOneAndDelete<Warp>(y=>y.Name == warpName);
				return true;
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Finds the warp with the given name.
		/// </summary>
		/// <param name="warpName">The name.</param>
		/// <returns>The warp, if it exists, or else null.</returns>
		public static Warp? Find(string warpName)
		{
			return warps.Find<Warp>(x => x.Name == warpName).FirstOrDefault();
		}

		/// <summary>
		/// Sets the position of a warp.
		/// </summary>
		/// <param name="warpName">The warp name.</param>
		/// <param name="x">The X position.</param>
		/// <param name="y">The Y position.</param>
		/// <returns>Whether the operation succeeded.</returns>
		public static bool Position(string warpName, int x, int y)
		{
			try
			{
				warps.FindOneAndUpdate<Warp>(x => x.Name == warpName, Builders<Warp>.Update.Set(x => x.Position, new Point(x, y)));
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
			return false;
		}

		/// <summary>
		/// Sets the hidden state of a warp.
		/// </summary>
		/// <param name="warpName">The warp name.</param>
		/// <param name="state">The state.</param>
		/// <returns>Whether the operation succeeded.</returns>
		public static async Task<bool> Hide(string warpName, bool state)
		{
			try
			{
				warps.FindOneAndUpdate<Warp>(x => x.Name == warpName, Builders<Warp>.Update.Set(x => x.IsPrivate, state));
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
			return false;
		}
	}

	/// <summary>
	/// Represents a warp.
	/// </summary>
	public class Warp
	{
		public ObjectId ObjectId { get; set; }
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets or sets the warp's privacy state.
		/// </summary>
		public bool IsPrivate { get; set; }
		/// <summary>
		/// Gets or sets the position.
		/// </summary>
		public Point Position { get; set; }

		public Warp(Point position, string name, bool isPrivate = false)
		{
			Name = name;
			Position = position;
			IsPrivate = isPrivate;
		}

		/// <summary>Creates a warp with a default coordinate of zero, an empty name, public.</summary>
		public Warp()
		{
			Position = Point.Zero;
			Name = "";
			IsPrivate = false;
		}
	}
}

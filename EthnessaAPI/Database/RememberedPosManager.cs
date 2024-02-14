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
using System.Data;
using System.Threading.Tasks;
using EthnessaAPI.Database.Models;
using Terraria;
using Microsoft.Xna.Framework;
using MongoDB.Driver;

namespace EthnessaAPI.Database
{
	public static class RememberedPosManager
	{
		private static IMongoCollection<RememberedPosition> rememberedPos => ServerBase.GlobalDatabase.GetCollection<RememberedPosition>("rememberedpos");
		public static Vector2? GetLeavePos(int accountId)
		{
			try
			{
				var pos = rememberedPos.Find<RememberedPosition>(x=>x.AccountId==accountId && x.WorldId==Main.worldID)
					.SortByDescending(x=>x.Created)
					.FirstOrDefault();

				if (pos is not null)
				{
					return pos.Position;
				}
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}

			return null;
		}

		public static void InsertLeavePos(int accountId, int X, int Y)
		{
			try
			{
				RememberedPosition pos = new(accountId, X, Y, Main.worldID);
				rememberedPos.InsertOne(pos);
			}
			catch (Exception ex)
			{
				ServerBase.Log.Error(ex.ToString());
			}
		}
	}
}

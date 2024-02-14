using System.Threading.Tasks;
using static EthnessaAPI.GetDataHandlers;

namespace EthnessaAPI.Handlers
{
	/// <summary>
	/// Handles emoji packets and checks for permissions
	/// </summary>
	public class EmojiHandler : IPacketHandler<GetDataHandlers.EmojiEventArgs>
	{
		/// <summary>
		/// Invoked when an emoji is sent in chat. Rejects the emoji packet if the player does not have emoji permissions
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnReceive(object sender, GetDataHandlers.EmojiEventArgs args)
		{
			if (!(args.Player.HasPermission(Permissions.sendemoji)))
			{
				args.Player.SendErrorMessage(GetString("You do not have permission to send emotes!"));
				args.Handled = true;
				return;
			}

		}
	}
}

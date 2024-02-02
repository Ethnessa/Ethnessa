using System.Threading.Tasks;
using static TShockAPI.GetDataHandlers;

namespace TShockAPI.Handlers
{
	/// <summary>
	/// Handles emoji packets and checks for permissions
	/// </summary>
	public class EmojiHandler : IPacketHandler<EmojiEventArgs>
	{
		/// <summary>
		/// Invoked when an emoji is sent in chat. Rejects the emoji packet if the player does not have emoji permissions
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public async Task OnReceive(EmojiEventArgs args)
		{
			if (!(await args.Player.HasPermission(Permissions.sendemoji)))
			{
				args.Player.SendErrorMessage(GetString("You do not have permission to send emotes!"));
				args.Handled = true;
				return;
			}

			await Task.CompletedTask;
		}
	}
}

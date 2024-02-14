using EthnessaAPI.Database.Models;

namespace EthnessaAPI.Hooks
{
	public class AccountDeleteEventArgs
	{
		public UserAccount Account { get; private set; }

		public AccountDeleteEventArgs(UserAccount account)
		{
			this.Account = account;
		}
	}

	public class AccountCreateEventArgs
	{
		public UserAccount Account { get; private set; }

		public AccountCreateEventArgs(UserAccount account)
		{
			this.Account = account;
		}
	}

	public class AccountHooks
	{
		public delegate void AccountCreateD(AccountCreateEventArgs e);
		public static event AccountCreateD AccountCreate;

		public static void OnAccountCreate(UserAccount u)
		{
			if (AccountCreate == null)
				return;

			AccountCreate(new AccountCreateEventArgs(u));
		}

		public delegate void AccountDeleteD(AccountDeleteEventArgs e);
		public static event AccountDeleteD AccountDelete;

		public static void OnAccountDelete(UserAccount u)
		{
			if (AccountDelete == null)
				return;

			AccountDelete(new AccountDeleteEventArgs(u));
		}
	}
}

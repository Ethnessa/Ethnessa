using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace EthnessaAPI
{
	public class HandlerList : HandlerList<EventArgs> { }

	public class HandlerList<T> where T : EventArgs
	{
		public class HandlerItem
		{
			public EventHandler<T> Handler { get; set; }
			public HandlerPriority Priority { get; set; }
			public bool GetHandled { get; set; }
		}

		private readonly object _handlerLock = new();
		private List<HandlerItem> _handlers { get; set; }
		public HandlerList()
		{
			_handlers = new List<HandlerItem>();
		}

		/// <summary>
		/// Register a handler
		/// </summary>
		/// <param name="handler">Delegate to be called</param>
		/// <param name="priority">Priority of the delegate</param>
		/// <param name="gethandled">Should the handler receive a call even if it has been handled</param>
		public void Register(EventHandler<T> handler, HandlerPriority priority = HandlerPriority.Normal, bool gethandled = false)
		{
			Register(Create(handler, priority, gethandled));
		}

		public void Register(HandlerItem obj)
		{
			lock (_handlerLock)
			{
				_handlers.Add(obj);
				_handlers = _handlers.OrderBy(h => (int)h.Priority).ToList();
			}
		}

		public void UnRegister(EventHandler<T> handler)
		{
			lock (_handlerLock)
			{
				_handlers.RemoveAll(h => h.Handler.Equals(handler));
			}
		}

		public void Invoke(object sender, T e)
		{
			List<HandlerItem> handlersSnapshot;
			lock (_handlerLock)
			{
				handlersSnapshot = new List<HandlerItem>(_handlers);
			}

			foreach (var handlerItem in handlersSnapshot)
			{
				if (e is HandledEventArgs hargs && hargs.Handled && !handlerItem.GetHandled)
				{
					continue;
				}
				handlerItem.Handler(sender, e);
			}
		}

		public static HandlerItem Create(EventHandler<T> handler, HandlerPriority priority = HandlerPriority.Normal, bool gethandled = false)
		{
			return new HandlerItem { Handler = handler, Priority = priority, GetHandled = gethandled };
		}
		public static HandlerList<T> operator +(HandlerList<T> hand, HandlerItem obj)
		{
			if (hand == null)
				hand = new HandlerList<T>();

			hand.Register(obj);
			return hand;
		}
		public static HandlerList<T> operator +(HandlerList<T> hand, EventHandler<T> handler)
		{
			if (hand == null)
				hand = new HandlerList<T>();

			hand.Register(Create(handler));
			return hand;
		}
		public static HandlerList<T> operator -(HandlerList<T> hand, HandlerItem obj)
		{
			return hand - obj.Handler;
		}
		public static HandlerList<T> operator -(HandlerList<T> hand, EventHandler<T> handler)
		{
			if (hand == null)
				return null;

			hand.UnRegister(handler);
			return hand;
		}
	}

	public enum HandlerPriority
	{
		Highest = 1,
		High = 2,
		Normal = 3,
		Low = 4,
		Lowest = 5,
	}
}

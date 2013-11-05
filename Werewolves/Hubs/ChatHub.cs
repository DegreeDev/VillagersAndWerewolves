using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace Werewolves.Hubs
{
	public class ChatHub : Hub
	{
		public async Task HandleMessage(string name, string message)
		{
			await Clients.All.message(name, message);
		}

		public override Task OnConnected()
		{
			return base.OnConnected();
		}
		public override Task OnDisconnected()
		{
			return base.OnDisconnected();
		}
		public override Task OnReconnected()
		{
			return base.OnReconnected();
		}
		public async Task JoinGroup(string group)
		{
			if (!string.IsNullOrWhiteSpace(group))
			{
				await Groups.Add(Context.ConnectionId, group);
			}
		}

	}
}
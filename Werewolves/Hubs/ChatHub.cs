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
	}
}
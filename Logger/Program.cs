using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using SignalRHubConnection = Microsoft.AspNet.SignalR.Client.HubConnection;

namespace Logger
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.Title = "Chat Console";

			var connection = new SignalRHubConnection("http://villagersandwerewolves.azurewebsites.net/");

			var hub = connection.CreateHubProxy("chatHub");

			Task.Run(async () =>
			{
				await connection.Start();

				hub.On<string>("message", message =>
				{
					Console.WriteLine(message);
				});

				await hub.Invoke("JoinGroup", "logger.page");

				Console.WriteLine("ConnectionId: " + connection.ConnectionId);
			});
			Console.WriteLine("type 'quit' to exit");

			while (true)
			{
				var message = Console.ReadLine();

				if (message == "quit")
				{
					break;
				}
			}
		}
	}
}

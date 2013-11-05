using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using SignalRHubConnection = Microsoft.AspNet.SignalR.Client.HubConnection;

namespace ChatApp
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.Title = "Chat Console";

			var connection = new SignalRHubConnection("http://villagersandwerewolves.azurewebsites.net/");

			var hub = connection.CreateHubProxy("chatHub");

			Console.WriteLine("What is your name?");
			var username = Console.ReadLine();

			Task.Run(async () =>
			{
				await connection.Start();
				hub.On<string, string>("message", (name, message) =>
				{
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.Write(name + ": ");
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write(message);
					Console.WriteLine();
				});
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
				Task.Run(async () =>
				{
					await hub.Invoke("handleMessage", username, message);
				});

			}
		}
	}
}

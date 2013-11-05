using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using SignalRHubConnection = Microsoft.AspNet.SignalR.Client.HubConnection;

namespace ViewerConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			var connection = new SignalRHubConnection("http://villagersandwerewolves.azurewebsites.net/");
			
			//var connection = new SignalRHubConnection("http://localhost:38369/");

			var hub = connection.CreateHubProxy("wereWolfHub");
			Task.Run(async () =>
				{
					await connection.Start();
					hub.On<string, string>("message", (name, message) =>
					{
						Console.WriteLine(name + ": " + message);
					});

					Console.WriteLine("ConnectionId: " + connection.ConnectionId);
					await hub.Invoke("joinViewers");
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
					await hub.Invoke("GeneralChat", message);
				});

			}

		}
	}
}

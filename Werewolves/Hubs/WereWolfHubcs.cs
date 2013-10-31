using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Werewolves.Models;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading.Tasks;

namespace Werewolves
{
	public class WereWolfHub : Hub
	{
		private static GameModel game = new GameModel();

		public async Task<PlayerGameInfoModel> JoinGame(string name, string id)
		{
			if (!string.IsNullOrWhiteSpace(id))
			{
				var player = game.FindByConnectionId(id);
				player.ConnectionId = this.Context.ConnectionId;
				foreach (var group in player.Groups)
				{
					await Groups.Add(this.Context.ConnectionId, group);
				}
				await Clients.Caller.message("Admin", "Welcome back!");

				return new PlayerGameInfoModel() { GameId = game.Id, PlayerId = player.ConnectionId };
			}

			if (!game.IsStarted)
			{
				var player = new PlayerModel()
				{
					ConnectionId = this.Context.ConnectionId,
					Name = name,
					Groups = new List<string>() { "players" }
				};
				game.Players.Add(player);

				await Clients.Caller.message("Admin", "You've joined the game!");
				await Clients.Others.message("Admin", name + " has joined the game");
				await Groups.Add(this.Context.ConnectionId, "players");

				return new PlayerGameInfoModel() { GameId = game.Id, PlayerId = player.ConnectionId};
				
			}
			else
			{
				 var player = new PlayerModel()
				{
					ConnectionId = this.Context.ConnectionId,
					Name = name,
					Groups = new List<string>() { "viewers" }
				};
				await Groups.Add(player.ConnectionId, "viewers");
				await Clients.Caller.error("You cannot join this game because it is already started, you have been added to the viewers");
				await Clients.Caller.message("Admin", "Welcome to the game, you're a viewer, please wait until this game is over.");

				return new PlayerGameInfoModel() { GameId = game.Id, PlayerId = player.ConnectionId };
			}
		}
		
		public async Task SetName(string name)
		{
			var player = game.FindByConnectionId(this.Context.ConnectionId);
			if (player != null && string.IsNullOrWhiteSpace(player.Name))
			{
				player.Name = name;
			}
		}

		public override Task OnDisconnected()
		{
			game.Players.Remove(game.Players.FirstOrDefault(x => x.ConnectionId == this.Context.ConnectionId));
			return base.OnDisconnected();
		}
		public override Task OnConnected()
		{
			return base.OnConnected();
		}
		public override Task OnReconnected()
		{
			return base.OnReconnected();
		}
	   
		public async Task StartGame()
		{
			game.IsStarted = true;

			await Clients.Group("players").message("Admin", "The game has started...let the lynching begin");


			var r1 = new Random().Next(game.Players.Count());
			var r2 = new Random().Next(game.Players.Count());

			while (r1 == r2)
			{
				r1 = new Random().Next(game.Players.Count());
			}


			var werewolve1 = game.Players.ElementAt(r1);
			var werewolve2 = game.Players.ElementAt(r2);

			var wolves = new List<string>(){ 
				werewolve1.ConnectionId, 
				werewolve2.ConnectionId
			
			};

			game.Players = game.Players.Select(x =>
			{
				x.IsWerewolf = wolves.Contains(x.ConnectionId);
				x.Groups.Add("werewolves");
				return x;
			}).ToList();

			
			
			await Task.WhenAll(
				Groups.Add(werewolve1.ConnectionId, "werewolves"),
				Groups.Add(werewolve2.ConnectionId, "werewolves"),
				Clients.Group("werewolves").setWerewolf(),
				Clients.Group("werewolves").message("Admin", "You have been selected as a Werewolf. A you now have a chat window for your other werewolf brethren"),
				Clients.Group("players").initiateVote()
			);
		}

		public async Task WereWolfChat(string message)
		{
			var name = game.FindByConnectionId(this.Context.ConnectionId).Name;
			await Clients.Group("werewolves").werewolfMessage(name, message);
		}
		public async Task GeneralChat(string message)
		{
			var name = game.FindByConnectionId(this.Context.ConnectionId).Name;

			await Clients.All.message(name, message);
		}
		
		public async Task CastVote(string player)
		{
			//get the current and player which that player voted for
			var currentPlayer = game.FindByConnectionId(this.Context.ConnectionId);
			var votedPlayer = game.FindByConnectionId(player);

			await Clients.Group("viewers").message("Admin", currentPlayer.Name + " voted for " + votedPlayer.Name);

			game.Votes.Add(player);

			await Clients.All.updateVoting(((double)game.Votes.Count() / (double)game.Players.Count()) * 100);

			if (game.Votes.Count() == game.Players.Count())
			{
				Task.WhenAll(
					Clients.All.updateVoting(0),
					Clients.All.message("Admin", "All votes have been cast. Voting is now closed"),
					Clients.Group("players").votingClosed()
				);
				game.IsVotingOpen = false;
				
				var winner = (from v in game.Votes
							  group v by v into g
							  select new
							  {
								  count = g.Count(),
								  player = g.Key,

							  }).OrderByDescending(x => x.count).First();

				var votedOffPlayer = game.FindByConnectionId(winner.player);

				await Clients.Client(player).message("Admin", "You've been lynched. Sorry.");
				game.Players.Remove(votedOffPlayer);
				
				await Groups.Remove(votedOffPlayer.ConnectionId, "players");
				votedOffPlayer.Groups.Remove("players");

				await Groups.Add(votedOffPlayer.ConnectionId, "viewers");
				votedOffPlayer.Groups.Add("viewers");

				await this.Clients.Group("players").message("Admin", votedOffPlayer.Name + " has been lynched");

				game.ResetVotes();

				if (game.Players.Count() == 2)
				{
					if (game.Players.Any(x => x.IsWerewolf))
					{
						await Clients.All.message("Admin", "The Werewolves have won!");
					}
					else
					{
						await Clients.All.message("Admin", "The Villagers have won!");
					}
				}
				else
				{
					await Clients.Group("players").initiateVote();
				}
			}
		}

		public async Task GetCurrentPlayers()
		{
			await Clients.Caller.processPlayers(
				game.Players
					.Where(x=>x.ConnectionId != this.Context.ConnectionId)
					.Select(x => new
						{
							id = x.ConnectionId,
							name = x.Name
						}
					)
			);
		}

		public async Task JoinViewers()
		{
			await Groups.Add(this.Context.ConnectionId, "viewers");

		}
	}
}
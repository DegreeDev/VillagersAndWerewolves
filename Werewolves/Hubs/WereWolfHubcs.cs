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
		private GameModel _game = MvcApplication.Game;

		public async Task<PlayerGameInfoModel> JoinGame(string name, string id)
		{
			if (!string.IsNullOrWhiteSpace(id))
			{
				var player = _game.FindByConnectionId(id);
				player.ConnectionId = this.Context.ConnectionId;
				foreach (var group in player.Groups)
				{
					await Groups.Add(this.Context.ConnectionId, group);
				}
				await Clients.Caller.message("Admin", "Welcome back!");

				return new PlayerGameInfoModel() { GameId = _game.Id, PlayerId = player.ConnectionId };
			}

			if (!_game.IsStarted)
			{
				var player = new PlayerModel()
				{
					ConnectionId = this.Context.ConnectionId,
					Name = name,
					Groups = new List<string>() { "players" }
				};
				_game.Players.Add(player);

				await Clients.Caller.message("Admin", "You've joined the game!");
				await Clients.Others.message("Admin", name + " has joined the game");
				await Groups.Add(this.Context.ConnectionId, "players");

				return new PlayerGameInfoModel() { GameId = _game.Id, PlayerId = player.ConnectionId};
				
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

				return new PlayerGameInfoModel() { GameId = _game.Id, PlayerId = player.ConnectionId };
			}
		}
		
		public async Task SetName(string name)
		{
			var player = _game.FindByConnectionId(this.Context.ConnectionId);
			if (player != null && string.IsNullOrWhiteSpace(player.Name))
			{
				player.Name = name;
			}
		}

		public override Task OnDisconnected()
		{
			_game.Players.Remove(_game.Players.FirstOrDefault(x => x.ConnectionId == this.Context.ConnectionId));
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
			await _game.StartGame();
		}

		public async Task WereWolfChat(string message)
		{
			var name = _game.FindByConnectionId(this.Context.ConnectionId).Name;
			await Clients.Group("werewolves").werewolfMessage(name, message);
		}
		public async Task GeneralChat(string message)
		{
			var player = _game.FindByConnectionId(this.Context.ConnectionId);
			string name = player == null ? "Console Man" : player.Name;
			await Clients.All.message(name, message);
		}
		
		public async Task CastVote(string player)
		{
			//get the current and player which that player voted for
			var currentPlayer = _game.FindByConnectionId(this.Context.ConnectionId);
			var votedPlayer = _game.FindByConnectionId(player);

			await _game.ProcessVote(currentPlayer, votedPlayer);

			if (_game.IsVotingOver)
			{
				await _game.ProccessVoteEnd();
			}
		}

		public async Task GetCurrentPlayers()
		{
			await Clients.Caller.processPlayers(
				_game.Players
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
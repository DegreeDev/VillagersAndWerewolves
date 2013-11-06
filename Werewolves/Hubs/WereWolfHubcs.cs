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
		private string _adminGravatar;

		public WereWolfHub()
		{
			_adminGravatar = "http://gravatar.com/avatar/ksjdlfjal?s=20";
		}

		private async Task Message(string name, string message, dynamic who, string gravatar)
		{
			await who.message(name, message, gravatar);
		}
        private async Task AdminMessage(string message, dynamic who)
        {
            await who.message("Admin", message, _adminGravatar);
        }

		public async Task<PlayerGameInfoModel> JoinGame(string name, string email, string id)
		{
			PlayerModel player;

			if (!string.IsNullOrWhiteSpace(id))
			{
				player = _game.FindByConnectionId(id);
				player.ConnectionId = Context.ConnectionId;
				foreach (var group in player.Groups)
				{
					await Groups.Add(Context.ConnectionId, group);
				}
				await AdminMessage("Welcome back!", Clients.Caller);
			}
			else if (!_game.IsStarted)
			{
				player = new PlayerModel
				{
					ConnectionId = Context.ConnectionId,
					Name = name,
					Email = email,
					Groups = new List<string>() { "players" }
				};
				_game.Players.Add(player);

				await Message("Admin", "You've joined the game!", Clients.Caller, _adminGravatar);
				await Message("Admin", name + " has joined the game", Clients.Others, _adminGravatar);
				await Groups.Add(Context.ConnectionId, "players");
			}
			else
			{
				player = new PlayerModel
				{
					ConnectionId = Context.ConnectionId,
					Name = name,
					Email = email,
					Groups = new List<string>() { "viewers" }
				};

				await Groups.Add(player.ConnectionId, "viewers");
				
				await Task.WhenAll(
					await Clients.Caller.error("You cannot join this game because it is already started, you have been added to the viewers"),
					await Message("Admin","Welcome to the game, you're a viewer, please wait until this game is over.", Clients.Caller, _adminGravatar)
				);
			}
			var gravatar = await player.GetGravatar();
			return new PlayerGameInfoModel
			{
				gameId = _game.Id,
				playerId = player.ConnectionId,
				gravatar = gravatar
			};
		}

		public async Task SetName(string name)
		{
			var player = _game.FindByConnectionId(Context.ConnectionId);
			if (player != null && string.IsNullOrWhiteSpace(player.Name))
			{
				player.Name = name;
			}
		}

		public override Task OnDisconnected()
		{
			_game.Players.Remove(_game.Players.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId));
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
            _game.IsStarted = true;

            await Clients.Group("players").message("Admin", "The game has started...let the lynching begin", _adminGravatar);

            var r1 = new Random().Next(_game.Players.Count());
            var r2 = new Random().Next(_game.Players.Count());

            while (r1 == r2)
            {
                r1 = new Random().Next(_game.Players.Count());
            }


            PlayerModel werewolve1 = _game.Players.ElementAt(r1),
                        werewolve2 = _game.Players.ElementAt(r2);

            var wolves = new List<string>
			{ 
				werewolve1.ConnectionId, 
				werewolve2.ConnectionId
			};

            _game.Players = _game.Players.Select(x =>
            {
                x.IsWerewolf = wolves.Contains(x.ConnectionId);
                x.Groups.Add("werewolves");
                return x;
            }).ToList();

            await Groups.Add(werewolve1.ConnectionId, "werewolves");
            await Groups.Add(werewolve2.ConnectionId, "werewolves");
            
            
            await Task.WhenAll(
                Clients.Group("werewolves").setWerewolf(),
                Clients.Group("werewolves").message("Admin", "You have been selected as a Werewolf. A you now have a chat window for your other werewolf brethren", _adminGravatar),
                Clients.Group("players").initiateVote()
            );
		}

		public async Task WereWolfChat(string message)
		{
			var player = _game.FindByConnectionId(Context.ConnectionId);
			var gravatar = await player.GetGravatar();
			await Clients.Group("werewolves").werewolfMessage(player.Name, message, gravatar);
		}
		public async Task GeneralChat(string message)
		{
			var player = _game.FindByConnectionId(Context.ConnectionId);
			string name = player == null ? "Console Man" : player.Name;
			await Message(name, message, Clients.All, await player.GetGravatar());
		}

		public async Task CastVote(string player)
		{
			//get the current and player which that player voted for
			var currentPlayer = _game.FindByConnectionId(Context.ConnectionId);
			var votedPlayer = _game.FindByConnectionId(player);

			_game.Votes.Add(votedPlayer.ConnectionId);

			await Clients.Group("viewers").message("Admin", currentPlayer.Name + " voted for " + votedPlayer.Name, _adminGravatar);
            await Clients.All.updateVoting(_game.VotingPercentage);

			if (_game.IsVotingOver)
			{
               var winner = (from v in _game.Votes
                              group v by v into g
                              select new
                              {
                                  count = g.Count(),
                                  player = g.Key,

                              }).OrderByDescending(x => x.count).First();

                var votedOffPlayer = _game.FindByConnectionId(winner.player);

				await Clients.Client(winner.player).message("Admin", "You've been lynched. Sorry.", _adminGravatar);
                _game.Players.Remove(votedOffPlayer);

                await Groups.Remove(votedOffPlayer.ConnectionId, "players");
                votedOffPlayer.Groups.Remove("players");

                await Groups.Add(votedOffPlayer.ConnectionId, "viewers");
                votedOffPlayer.Groups.Add("viewers");

				await Clients.Group("players").message("Admin", votedOffPlayer.Name + " has been lynched", _adminGravatar);

                _game.ResetVotes();

				await Clients.All.updateVoting(_game.VotingPercentage);

                if (_game.Players.Count() == 2)
                {
                    if (_game.Players.Any(x => x.IsWerewolf))
                    {
                        await Clients.All.message("Admin", "The Werewolves have won!", _adminGravatar);
                    }
                    else
                    {
						await Clients.All.message("Admin", "The Villagers have won!", _adminGravatar);
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
				_game.Players
					.Where(x => x.ConnectionId != Context.ConnectionId)
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
			await Groups.Add(Context.ConnectionId, "viewers");

		}
	}
}
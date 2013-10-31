using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace Werewolves.Models
{
	public class PlayerModel
	{
		public bool IsWerewolf { get; set; }
		public string ConnectionId { get; set; }
		public string Name { get; set; }
		public IList<string> Groups { get; set; }

		public PlayerModel()
		{
			this.Groups = new List<string>();
		}
	}
	public class PlayerGameInfoModel
	{
		public Guid GameId { get; set; }
		public string PlayerId { get; set; }
	}
	public class GameModel
	{
		private IHubContext _context;
		private IList<PlayerModel> _players;
		private readonly string _admin = "Admin";
		private IList<string> _votes; 

		public Guid Id { get; set; }
		public IList<PlayerModel> Players { get { return _players; } }
		public IList<string> Votes { get { return _votes; } }

		public bool IsVotingOver { get { return _votes.Count() == _players.Count(); } }

		public bool IsStarted { get; set; }
		public bool IsVotingOpen { get; set; }

		public GameModel(IHubContext context)
		{
			_context = context;
			_players = new List<PlayerModel>();
			_votes = new List<string>();
		}

		public void ResetVotes()
		{
			_votes.Clear();
		}

		public async Task AddPlayer(PlayerModel player)
		{
			await _context.Clients.All.message(_admin, player.Name + " has joined the game");
			_players.Add(player);
		}

		public async Task ProcessVote(PlayerModel currentPlayer, PlayerModel votedPlayer)
		{
			await _context.Clients.Group("viewers").message("Admin", currentPlayer.Name + " voted for " + votedPlayer.Name);
			await _context.Clients.All.updateVoting(((double)_votes.Count() / (double)_players.Count()) * 100);
		}

		public async Task ProccessVoteEnd()
		{
			Task.WhenAll(
					_context.Clients.All.updateVoting(0),
					_context.Clients.All.message("Admin", "All votes have been cast. Voting is now closed"),
					_context.Clients.Group("players").votingClosed()
				);
			this.IsVotingOpen = false;

			var winner = (from v in _votes
						  group v by v into g
						  select new
						  {
							  count = g.Count(),
							  player = g.Key,

						  }).OrderByDescending(x => x.count).First();

			var votedOffPlayer = this.FindByConnectionId(winner.player);

			await _context.Clients.Client(winner.player).message("Admin", "You've been lynched. Sorry.");
			_players.Remove(votedOffPlayer);

			await _context.Groups.Remove(votedOffPlayer.ConnectionId, "players");
			votedOffPlayer.Groups.Remove("players");

			await _context.Groups.Add(votedOffPlayer.ConnectionId, "viewers");
			votedOffPlayer.Groups.Add("viewers");

			await _context.Clients.Group("players").message("Admin", votedOffPlayer.Name + " has been lynched");

			this.ResetVotes();

			if (_players.Count() == 2)
			{
				if (_players.Any(x => x.IsWerewolf))
				{
					await _context.Clients.All.message("Admin", "The Werewolves have won!");
				}
				else
				{
					await _context.Clients.All.message("Admin", "The Villagers have won!");
				}
			}
			else
			{
				await _context.Clients.Group("players").initiateVote();
			}
		}

		public PlayerModel FindByConnectionId(string connectionId)
		{
			return this.Players.FirstOrDefault(x => x.ConnectionId == connectionId);
		}

		public async Task SetWolves()
		{
			this.IsStarted = true;

			await _context.Clients.Group("players").message("Admin", "The game has started...let the lynching begin");

			var r1 = new Random().Next(_players.Count());
			var r2 = new Random().Next(_players.Count());

			while (r1 == r2)
			{
				r1 = new Random().Next(_players.Count());
			}


			PlayerModel werewolve1 = _players.ElementAt(r1),
						werewolve2 = _players.ElementAt(r2);

			var wolves = new List<string>
			{ 
				werewolve1.ConnectionId, 
				werewolve2.ConnectionId
			};

			_players = _players.Select(x =>
			{
				x.IsWerewolf = wolves.Contains(x.ConnectionId);
				x.Groups.Add("werewolves");
				return x;
			}).ToList();

			await Task.WhenAll(
				_context.Groups.Add(werewolve1.ConnectionId, "werewolves"),
				_context.Groups.Add(werewolve2.ConnectionId, "werewolves"),
				_context.Clients.Group("werewolves").setWerewolf(),
				_context.Clients.Group("werewolves").message("Admin", "You have been selected as a Werewolf. A you now have a chat window for your other werewolf brethren"),
				_context.Clients.Group("players").initiateVote()
			);
		}
	}
}
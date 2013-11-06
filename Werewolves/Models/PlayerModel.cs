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
		public string Email { get; set; }
		public string Name { get; set; }
		public IList<string> Groups { get; set; }

		public async Task<string> GetGravatar()
		{
			return await Task.FromResult<string>(GravatarService.Get(Email, 80));
		}

		public PlayerModel()
		{
			this.Groups = new List<string>();
		}
	}
	public class PlayerGameInfoModel
	{
		public Guid gameId { get; set; }
		public string playerId { get; set; }
		public string gravatar { get; set; }
	}
	public class GameMessageModel
	{
		public string Name { get; set; }
		public string Message { get; set; }
		public string Gravatar { get; set; }
	}

	public class GameModel
	{
		private IHubContext _context;
        private readonly string _admin = "Admin";
		

		public Guid Id { get; set; }
        public IList<PlayerModel> Players { get; set; }
        public IList<string> Votes { get; set; }

        public bool IsVotingOver { get { return Votes.Count() == Players.Count(); } }
        public double VotingPercentage
        {

            get
            {
                return (double)Votes.Count() / (double)Players.Count() * 100;
            }
        }

		public bool IsStarted { get; set; }
		public bool IsVotingOpen { get; set; }

		public GameModel(IHubContext context)
		{
			_context = context;
            Players = new List<PlayerModel>();
            Votes = new List<string>();
		}

		public void ResetVotes()
		{
            Votes.Clear();
		}

		public PlayerModel FindByConnectionId(string connectionId)
		{
			return this.Players.FirstOrDefault(x => x.ConnectionId == connectionId);
		}
        	
	}
}
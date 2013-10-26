using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Werewolves.Models
{
    public class PlayerModel
    {
        public Guid Id { get; set; }
        public bool IsWerewolf { get; set; }
        public string ConnectionId { get; set; }
        public string Name { get; set; }
    }
    public class GameModel
    {
        public Guid Id { get; set; }
        public IList<PlayerModel> Players { get; set; }
        public bool IsStarted { get; set; }
        public bool IsVotingOpen { get; set; }
        public List<string> Votes { get; set; }

        public GameModel()
        {
            this.Players = new List<PlayerModel>();
            this.Votes = new List<string>();
        }
        public void ResetVotes()
        {
            this.Votes = new List<string>();
        }
        public PlayerModel FindByConnectionId(string connectionId)
        {
            return this.Players.FirstOrDefault(x => x.ConnectionId == connectionId);
        }
    }
}
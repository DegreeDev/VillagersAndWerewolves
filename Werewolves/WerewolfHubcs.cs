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

        
        public async Task JoinGame(string name)
        {
            if (!game.IsStarted)
            {
                game.Players.Add(new PlayerModel()
                {
                    ConnectionId = this.Context.ConnectionId,
                    Id = Guid.NewGuid(),
                    Name = name
                });
                await Clients.Caller.message("You've joined the game!");
                await Clients.Others.message(name + " has joined the name");
                await Groups.Add(this.Context.ConnectionId, "players");
            }
            else
            {
                await Clients.Caller.error("You cannot join this game becuase it is already started");
            }
        }
        public void SetName(string name)
        {
            var player = game.FindByConnectionId(this.Context.ConnectionId);
            if (player != null && string.IsNullOrWhiteSpace(player.Name))
            {
                player.Name = name;
            }
        }

        public override System.Threading.Tasks.Task OnDisconnected()
        {
            game.Players.Remove(game.Players.FirstOrDefault(x => x.ConnectionId == this.Context.ConnectionId));
            return base.OnDisconnected();
        }
        public override System.Threading.Tasks.Task OnConnected()
        {
            return base.OnConnected();
        }
        public async Task StartGame()
        {
            game.IsStarted = true;

            await Clients.Group("players").message("The game has started...let the lynching begin");


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
                return x;
            }).ToList();

            await Groups.Add(werewolve1.ConnectionId, "werewolves");
            await Groups.Add(werewolve2.ConnectionId, "werewolves");

            await Clients.Group("werewolves").setWerewolf();

            await Clients.Group("werewolves").message("You have been selected as a Werewolf. A you now have a chat window for your other werewolf brethren");
           
            await Clients.Group("players").initiateVote();
        }

        public async Task WereWolfChat(string message)
        {
            var name = game.FindByConnectionId(this.Context.ConnectionId).Name;
            await Clients.Group("werewolves").werewolfMessage(name + ": " + message);
        }
        public async Task GeneralChat(string message)
        {
            var name = game.FindByConnectionId(this.Context.ConnectionId).Name;
            await Clients.All.message(name + ": " + message);
        }
        public async Task CastVote(string player)
        {
            var currentPlayer = game.FindByConnectionId(this.Context.ConnectionId);
            var votedPlayer = game.FindByConnectionId(player);


            await Clients.Group("viewers").message(currentPlayer.Name + " voted for " + votedPlayer.Name);

            game.Votes.Add(player);

            if (game.Votes.Count() == game.Players.Count())
            {
                await Clients.All.message("All votes have been cast. Voting is now closed");
                await Clients.Group("players").votingClosed();
                game.IsVotingOpen = false;
                var winner = (from v in game.Votes
                              group v by v into g
                              select new
                              {
                                  count = g.Count(),
                                  player = g.Key,

                              }).OrderByDescending(x => x.count).First();

                var votedOffPlayer = game.FindByConnectionId(winner.player);

                await this.Clients.Client(player).message("You've been lynched. Sorry.");
                game.Players.Remove(votedOffPlayer);
                await Groups.Remove(votedOffPlayer.ConnectionId, "players");
                await Groups.Add(votedOffPlayer.ConnectionId, "viewers");
                await this.Clients.AllExcept(player).message(votedOffPlayer.Name + " has been lynched");
                game.ResetVotes();

                if (game.Players.Count() == 2)
                {
                    if (game.Players.Any(x => x.IsWerewolf))
                    {
                        await Clients.All.message("The Werewolves have won!");
                    }
                    else
                    {
                        await Clients.All.message("The Villagers have won!");
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
            await Clients.Caller.processPlayers(game.Players.Select(x => new
            {
                id = x.ConnectionId,
                name = x.Name
            }));
        }

    }
}
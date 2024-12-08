﻿using BlazorGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorGame.Data
{
    public enum GameStatus
    {
        None,
        Open,
        Complete
    }

    public class Game
    {
        List<Player> _players = new();
        List<Card> _cards = new();
        private int _currentTurnIndex = -1;

        public Guid Id { get; }
        public int PinCode { get; }
        public bool HasDealtCards => _currentTurnIndex >= 0;
        public GameStatus State { get; private set; } = GameStatus.None;
        public List<Player> Players { get => _players; }
        public string GameCreatorId { get; init; }
        public string GameCreatorName { get; init; }
        public bool ReadyForNextTurn => PlayedCards.Count == Players.Count;
        public Dictionary<string, Card> PlayedCards { get; private set; } = new();
        public Game(int pinCode, Player creator, ICardProvider cardProvider)
        {
            Id = Guid.NewGuid();
            PinCode = pinCode;
            _players.Add(creator);
            GameCreatorId = creator.UserId;
            GameCreatorName = creator.Name;
            State = GameStatus.Open;
            _cards = cardProvider.Cards();
        }
        public void PlayCard(string userId, Card card)
        {
            if (PlayedCards.ContainsKey(userId))
            {
                throw new InvalidOperationException("Player has already played a card this turn.");
            }

            PlayedCards[userId] = card;
            var player = Players.Single(x => x.UserId == userId);
            player.Hand.Remove(card);
            
            if (ReadyForNextTurn)
            {
                //TODO: Implement logic for determining the winner 
            }
        }
        
        
        public string ActivePlayerId
        {
            get
            {
                if (_currentTurnIndex < 0)
                    return "";

                _currentTurnIndex %= _players.Count;

                return _players.Skip(_currentTurnIndex).Take(1).First().UserId;
            }
        }

        public void CompleteTurn()
        {
            if (_currentTurnIndex < 0)
                throw new InvalidOperationException("Cards have not been dealt.");

            if (++_currentTurnIndex == _players.Count)
                _currentTurnIndex = 0;
            
            PlayedCards.Clear();
        }

        public void DealCards()
        {
            if (HasDealtCards)
                throw new InvalidOperationException("Cards have been dealt");

            if (State is GameStatus.Complete)
            {
                throw new InvalidOperationException("Game is closed");
            }

            if (!_players.Any())
            {
                return;
            }

            _players.ForEach(p => p.Hand.Clear());
            int i = 0;

            Shuffle().ForEach((card) => {
                var p = i % _players.Count;
                _players[p].Hand.Add(card);
                i++;
            });

            _currentTurnIndex = 0;
        }

        public Game Reset()
        {
            _currentTurnIndex = -1;
            return this;
        }


        public Game AddPlayer(Player player)
        {
            if (_players.Any(x => x.UserId == player.UserId))
                throw new InvalidOperationException("Player has already joined the game");

            if (_players.Any(x => x.Name == player.Name))
                throw new InvalidOperationException("Username is already in use");

            if (HasDealtCards)
                throw new InvalidOperationException("Cards have been dealt");

            _players.Add(player);
            return this;
        }

        private List<Card> Shuffle()
        {
            // TODO: Add shuffle logic
            return _cards;
        }

        public Player? RetirePlayer(string userId)
        {
            if (!_players.Any(x => x.UserId == userId))
                return null;

            var player = _players.Single(x => x.UserId == userId);
            
            var cards = player.Hand; // TODO: do something with these
            player.Hand.Clear();
            _players.Remove(player);

            return player;
        }

        public List<CardHand> Hands => Players.Select(x => new CardHand(x.UserId, x.Name)
        {
            Cards = x.Hand
        }).ToList();

        public string GetPlayerName(string userId)
        {
            var player = Players.FirstOrDefault(x => x.UserId == userId);
            return player is null ? "" : player.Name;
        }
        
        public Player? GetPlayerById(string userId)
        {
            var player = Players.FirstOrDefault(x => x.UserId == userId);
            return player;
        }
    }

    public class Card
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string Suit { get; set; }
    }

    public class Player
    {
        Game? _game;
        
        public string Name { get; init; }
        public string UserId { get; init; }

        public Player(string userId, string name)
        {
            UserId = userId;
            Name = name;
        }

        public List<Card> Hand { get; init; } = new();

        public void Join(Game game)
        {
            _game = game.AddPlayer(this);
        }

        public void LeaveGame()
        {
            if (_game is not null)
            {
                _game = null;
            }
        }
    }
}

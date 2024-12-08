using System;
using System.Collections.Generic;
using System.Linq;
using BlazorGame.Data;
using BlazorGame.Models;

public record GameStateModel
{
    private readonly Game _game;

    public GameStateModel(Game game)
    {
        _game = game;
        GameSessionId = game.Id;
        HasDealtCards = game.HasDealtCards;
        IsComplete = game.Players.All(p => !p.Hand.Any());
        GameCreatorId = game.GameCreatorId;
        GameCreatorName = game.GameCreatorName;
        PinCode = game.PinCode;
        Hands = game.Hands;
        PlayedCards = game.PlayedCards;
        ReadyForNextTurn = game.ReadyForNextTurn;
    }

    public Guid GameSessionId { get; }
    public bool HasDealtCards { get; }
    public bool IsComplete { get; }
    public string GameCreatorId { get; }
    public string GameCreatorName { get; }
    public string TurnWinnerId => _game.TurnWinnerId;
    public int PinCode { get; }
    public List<CardHand> Hands { get; }
    public Dictionary<string, Card> PlayedCards { get; }
    public bool ReadyForNextTurn { get; }
}
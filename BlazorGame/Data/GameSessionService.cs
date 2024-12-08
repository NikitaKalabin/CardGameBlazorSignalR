using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorGame.Data;
using BlazorGame.Hubs;
using BlazorGame.Models;
using Microsoft.AspNetCore.SignalR;

public class GameSessionService
{
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ICardProvider _cardProvider;
    private Dictionary<Guid, Game> _currentGames = new();

    public GameSessionService(IHubContext<GameHub> hubContext, ICardProvider cardProvider)
    {
        _hubContext = hubContext;
        _cardProvider = cardProvider;
    }

    public async Task<GameStateModel> CreateGame(string userId, string userName, int pinCode)
    {
        var player = new Player(userId, userName);
        var game = new Game(pinCode, player, _cardProvider);
        var gameState = new GameStateModel(game);

        _currentGames[game.Id] = game;

        await _hubContext.Groups.AddToGroupAsync(userId, game.Id.ToString());
        await _hubContext.Clients.Group(game.Id.ToString())
            .SendAsync("GameCreated", gameState);

        return gameState;
    }

    public async Task<GameStateModel?> JoinGame(string userId, string userName, Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            var player = new Player(userId, userName);
            player.Join(game);

            var gameState = new GameStateModel(game);

            await _hubContext.Groups.AddToGroupAsync(userId, game.Id.ToString());
            await _hubContext.Clients.Group(game.Id.ToString())
                .SendAsync("PlayerJoined", gameState);

            return gameState;
        }

        return null;
    }

    public async Task LeaveGame(string userId, Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            game.RetirePlayer(userId);

            await _hubContext.Groups.RemoveFromGroupAsync(userId, game.Id.ToString());
            await _hubContext.Clients.Group(game.Id.ToString())
                .SendAsync("PlayerRetired", new { UserId = userId });
        }
    }

    public async Task<GameStateModel?> GetCurrentState(Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            return await Task.FromResult(new GameStateModel(game));
        }

        return null;
    }

    public async Task<GameStateModel?> DealCards(string userId, Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            game.DealCards();
            var gameState = new GameStateModel(game);

            await _hubContext.Clients.Group(game.Id.ToString())
                .SendAsync("GameStateChanged", gameState);

            return gameState;
        }

        return null;
    }

    public async Task<string?> TryPlayCard(string userId, Card card, Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            try
            {
                game.PlayCard(userId, card);
                var gameState = new GameStateModel(game);

                await _hubContext.Clients.Group(game.Id.ToString())
                    .SendAsync("GameStateChanged", gameState);

                return null; // No error
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message; // Return the error message
            }
        }

        return "Game not found or invalid PIN code.";
    }
    

    private bool TryGetGame(Guid gameId, int pinCode, out Game? game)
    {
        game = null;
        var item = _currentGames.FirstOrDefault(x => x.Value.Id == gameId && x.Value.PinCode == pinCode);
        if (item.Key != Guid.Empty)
        {
            game = item.Value;
            return true;
        }

        return false;
    }
    
    public async Task RestartGame(string userId, Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            game.Reset();
            var gameState = new GameStateModel(game);

            await _hubContext.Clients.Group(game.Id.ToString())
                .SendAsync("GameStateChanged", gameState);
        }
    }

    public async Task NextTurn(string userId, Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            game.CompleteTurn();
            var gameState = new GameStateModel(game);

            await _hubContext.Clients.Group(game.Id.ToString())
                .SendAsync("GameStateChanged", gameState);
        }
    }
    
    public async Task<string?> GetPlayerNameById(string userId, Guid gameId, int pinCode)
    {
        if (TryGetGame(gameId, pinCode, out var game))
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.UserId == userId);
                return player != null ? player.Name : string.Empty;
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message; // Return the error message
            }
        }

        return "User not found or invalid PIN code.";
    }
    
}
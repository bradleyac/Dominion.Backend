using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Dominion.Backend;

public static class GameLogic
{
  public static async Task<GameState> StartGameAsync(GameState game)
  {
    if (game.GameStarted)
    {
      return game;
    }

    PlayerState[] players = [.. game.Players.Shuffle().Select((player, i) =>
    {
      var shuffledDeck = player.Deck.Shuffle();
      return player with
      {
        Hand = [.. shuffledDeck.Take(5)],
        Deck = [.. shuffledDeck.Skip(5)],
        Resources = i == 0 ? new PlayerResources(1, 1, 0, 0, 0, 0) : PlayerResources.Empty
      };
    })];

    return game with { GameStarted = true, Players = players, CurrentPlayer = 0, ActivePlayerId = players[0].Id };
  }

  public static async Task<GameState> EndTurnAsync(GameState game)
  {
    int nextPlayerIndex = game.CurrentPlayer + 1;
    int nextTurn = game.CurrentTurn;
    if (nextPlayerIndex >= game.Players.Length)
    {
      nextPlayerIndex = 0;
      nextTurn = game.CurrentTurn + 1;
    }

    var players = game.Players.Select((player, i) =>
    {
      if (i == game.CurrentPlayer)
      {
        return EndPlayerTurn(player);
      }
      else if (i == nextPlayerIndex)
      {
        return StartPlayerTurn(player);
      }
      else
      {
        return player;
      }
    }).ToArray();

    return game with { Phase = Phase.Action, Players = players, CurrentTurn = nextTurn, CurrentPlayer = nextPlayerIndex, ActivePlayerId = players[nextPlayerIndex].Id };
  }

  public static async Task<GameState> EndActionPhaseAsync(GameState game, string playerId)
  {
    if (game.Players[game.CurrentPlayer].Id == playerId && game.Phase == Phase.Action)
    {
      return game with { Phase = Phase.BuyOrPlay };
    }
    return game;
  }

  public static async Task<GameState> BuyCardAsync(GameState game, string playerId, int cardId)
  {
    if (IsActivePlayer(game, playerId, out var thisPlayer))
    {
      if (game.Phase.IsBuyPhase())
      {
        var cardPile = game.KingdomCards.FirstOrDefault(pile => pile.Card.Id == cardId);
        if (cardPile is not null && cardPile.Remaining > 0 && thisPlayer.Resources.Buys > 0 && thisPlayer.Resources.Coins >= cardPile.Card.Cost)
        {
          var cardInstance = new CardInstance(cardPile.Card);
          var newCardPile = cardPile with { Remaining = cardPile.Remaining - 1 };
          var newPlayer = thisPlayer with
          {
            Discard = [.. thisPlayer.Discard, cardInstance],
            Resources = thisPlayer.Resources with
            {
              Buys = thisPlayer.Resources.Buys - 1,
              Coins = thisPlayer.Resources.Coins - cardPile.Card.Cost
            }
          };

          game = game with
          {
            Phase = Phase.Buy,
            Players = game.Players.Select(player => player.Id == newPlayer.Id ? newPlayer : player).ToArray(),
            KingdomCards = [.. game.KingdomCards.Select(pile => pile.Card.Id == newCardPile.Card.Id ? newCardPile : pile)]
          };

          if (newPlayer.Resources.Buys == 0)
          {
            return await EndTurnAsync(game);
          }
        }
      }
    }

    return game;
  }

  public static async Task<GameState> PlayCardAsync(GameState game, string playerId, string cardInstanceId)
  {
    if (IsActivePlayer(game, playerId, out var thisPlayer))
    {
      if (HasCardInHand(thisPlayer, cardInstanceId, out var cardInstance))
      {
        if (game.Phase == Phase.Action && cardInstance.Card.Types.Contains(CardType.Treasure) && !cardInstance.Card.Types.Contains(CardType.Action))
        {
          game = game with { Phase = Phase.BuyOrPlay };
        }

        if ((game.Phase == Phase.Action && cardInstance.Card.Types.Contains(CardType.Action) && thisPlayer.Resources.Actions > 0)
          || (game.Phase == Phase.BuyOrPlay && cardInstance.Card.Types.Contains(CardType.Treasure)))
        {
          var newPlayer = thisPlayer with
          {
            Hand = [.. thisPlayer.Hand.Where(card => card.Id != cardInstanceId)],
            Play = [.. thisPlayer.Play, cardInstance],
            Resources = thisPlayer.Resources with { Actions = thisPlayer.Resources.Actions - (game.Phase == Phase.Action ? 1 : 0) },
          };

          game = game with
          {
            Players = game.Players.Select(player => player.Id == newPlayer.Id ? newPlayer : player).ToArray(),
            ResumeState = new PlayCardResumeState(cardInstance, 0, null)
          };

          CardEffectHandler[] effectHandlers = [new FluentEffectHandler()];

          for (int i = 0; i < cardInstance.Card.Effects.Length; i++)
          {
            game = game with { ResumeState = game.ResumeState! with { EffectIndex = i, EffectResumeState = null } };

            foreach (var handler in effectHandlers)
            {
              var effect = cardInstance.Card.Effects[i];
              if (handler.CanHandleEffect(effect))
              {
                (game, bool completed) = handler.HandleEffect(game, playerId, effect);
                if (!completed)
                {
                  return game;
                }
              }
            }
          }

          return game with { ResumeState = null, ActivePlayerId = game.Players[game.CurrentPlayer].Id };
        }
      }
    }

    return game;
  }

  public static async Task<GameState> ResumePlayingCard(GameState game, string playerId, ChosenCards chosenCards)
  {
    var resumeState = game.ResumeState;
    var cardInstance = resumeState!.CardInstance;
    var resumedEffect = cardInstance.Card.Effects[resumeState.EffectIndex];

    game = game with { Players = [.. game.Players.Select(p => p.Id == playerId ? p with { ActiveFilter = null } : p)] };

    CardEffectHandler[] effectHandlers = [new FluentEffectHandler()];

    foreach (var handler in effectHandlers)
    {
      if (handler.CanHandleEffect(resumedEffect))
      {
        (game, bool completed) = handler.ResumeHandleEffect(game, playerId, resumedEffect, chosenCards);
        if (!completed)
        {
          return game;
        }
      }
    }

    for (int i = resumeState.EffectIndex + 1; i < cardInstance.Card.Effects.Length; i++)
    {
      game = game with { ResumeState = game.ResumeState with { EffectIndex = i, EffectResumeState = null } };

      foreach (var handler in effectHandlers)
      {
        var effect = cardInstance.Card.Effects[i];
        if (handler.CanHandleEffect(effect))
        {
          (game, bool completed) = handler.HandleEffect(game, playerId, effect);
          if (!completed)
          {
            return game;
          }
        }
      }
    }

    return game with { ActivePlayerId = game.Players[game.CurrentPlayer].Id };
  }

  private static PlayerState StartPlayerTurn(PlayerState player)
  {
    return player with { Resources = PlayerResources.NewTurn(player.Resources) };
  }

  private static PlayerState EndPlayerTurn(PlayerState player)
  {
    player = player with
    {
      Discard = [.. player.Discard, .. player.Hand, .. player.Play],
      Play = [],
      Hand = [],
      Resources = PlayerResources.EndTurn(player.Resources)
    };
    player = Utils.DrawCards(player, 5);
    return player;
  }

  private static PlayerState? GetPlayerById(GameState game, string playerId) => game.Players.FirstOrDefault(player => player.Id == playerId);

  private static bool IsActivePlayer(GameState game, string playerId, [NotNullWhen(true)] out PlayerState? player)
  {
    var thisPlayer = GetPlayerById(game, playerId);

    if (thisPlayer is not null && playerId == game.ActivePlayerId)
    {
      player = thisPlayer;
      return true;
    }

    player = null;
    return false;
  }

  private static bool HasCardInHand(PlayerState player, string cardInstanceId, [NotNullWhen(true)] out CardInstance? cardInstance) => (cardInstance = player.Hand.FirstOrDefault(card => card.Id == cardInstanceId)) is not null;
}
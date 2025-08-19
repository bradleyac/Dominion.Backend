using static Dominion.Backend.CardZone;

namespace Dominion.Backend;

public record Game(string GameId, string DisplayName, string[] Players, string? ActivePlayerId);
public record GameStateViewModel(string GameId, string DisplayName, bool GameStarted, GameResult? GameResult, KingdomState KingdomState, TurnState TurnState, LogState Log, FullPlayerData Me, PartialPlayerData[] Opponents, string[] PlayerIds);
public record TurnState(string CurrentTurnPlayerId, string? ActivePlayerId, int Turn, string Phase);
public record LogState(string[] Messages);
public record KingdomState(CardPile[] Supply, CardInstanceDto[] Trash, CardInstanceDto[] Reveal);
public record CardPile(int CardId, int Count);
public record CardInstanceDto(string InstanceId, int CardId, CardZone Location);
public record EffectReference(CardInstanceDto CardInstance, string Prompt);
public record FullPlayerData(string PlayerId, CardInstanceDto[] Hand, int PlayAllTreasuresValue, int DeckCount, CardInstanceDto[] Discard, CardInstanceDto[] Play, CardInstanceDto[] PrivateReveal, PlayerResources Resources, PlayerChoice? ActiveChoice);
public record PartialPlayerData(string PlayerId, int HandCount, int DeckCount, int DiscardCount, int? DiscardFaceUpCardId, CardInstanceDto[] Play, PlayerResources Resources);
public record GameResult(string[] Winners, Dictionary<string, double> Scores);

public static partial class GameStateExtensions
{
  // TODO: How bad is this? We could split it up...
  public static GameStateViewModel ToPlayerGameStateViewModel(this GameState @this, string playerId)
  {
    var player = @this.Players.Single(player => player.Id == playerId);
    var opps = @this.Players.Where(player => player.Id != playerId);
    return new GameStateViewModel(
      GameId: @this.GameId,
      DisplayName: @this.DisplayName,
      GameStarted: @this.GameStarted,
      GameResult: @this.GameResult,
      KingdomState: new KingdomState([.. @this.KingdomCards.Select(kc => new CardPile(kc.Card.Id, kc.Remaining))], [.. @this.Trash.Select(ToDto(Trash))], [.. @this.Reveal.Select(ToDto(Reveal))]),
      TurnState: new TurnState(@this.Players[@this.CurrentPlayer].Id, @this.ActivePlayerId, @this.CurrentTurn, @this.Phase.ToString()),
      Log: new LogState(@this.Log),
      Me: new FullPlayerData(playerId, [.. player.Hand.Select(ToDto(Hand))], player.Hand.Sum(c => c.Card.CoinValue), player.Deck.Length, [.. player.Discard.Select(ToDto(Discard))], [.. player.Play.Select(ToDto(Play))], [.. player.PrivateReveal.Select(ToDto(PrivateReveal))], player.Resources, player.ActiveChoice),
      Opponents: [.. opps.Select(opp => new PartialPlayerData(opp.Id, opp.Hand.Length, opp.Deck.Length, opp.Discard.Length, opp.Discard.LastOrDefault()?.Card.Id, [.. opp.Play.Select(ToDto(Play))], opp.Resources))],
      PlayerIds: [.. @this.Players.Select(p => p.Id)]
    );
  }

  public static Func<CardInstance, CardInstanceDto> ToDto(CardZone location) => (CardInstance instance) => ToDto(instance, location);
  public static CardInstanceDto ToDto(this CardInstance @this, CardZone location) => new CardInstanceDto(@this.Id, @this.Card.Id, location);
}
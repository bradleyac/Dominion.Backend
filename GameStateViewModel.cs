namespace Dominion.Backend;

public record GameStateViewModel(string GameId, KingdomState KingdomState, TurnState TurnState, LogState Log, FullPlayerData Me, PartialPlayerData[] Opponents);
public record TurnState(string CurrentTurnPlayerId, string? ActivePlayerId, int Turn, string Phase);
public record LogState(string[] Messages);
public record KingdomState(CardPile[] Supply, CardInstanceDto[] Trash, CardInstanceDto[] Reveal);
public record CardPile(int CardId, int Count);
public record CardInstanceDto(string InstanceId, int CardId);
public record FullPlayerData(string PlayerId, CardInstanceDto[] Hand, int DeckCount, CardInstanceDto[] Discard, CardInstanceDto[] Play, CardInstanceDto[] PrivateReveal, PlayerResources Resources, CardFilter? ActiveCardChoice);
public record PartialPlayerData(string PlayerId, int HandCount, int DeckCount, int DiscardCount, int? DiscardFaceUpCardId, CardInstanceDto[] Play, PlayerResources Resources);

public static partial class GameStateExtensions
{
  // TODO: How bad is this? We could split it up...
  public static GameStateViewModel ToPlayerGameStateViewModel(this GameState @this, string playerId)
  {
    var player = @this.Players.Single(player => player.Id == playerId);
    var opps = @this.Players.Where(player => player.Id != playerId);
    return new GameStateViewModel(
      GameId: @this.GameId,
      KingdomState: new KingdomState([.. @this.KingdomCards.Select(kc => new CardPile(kc.Card.Id, kc.Remaining))], [.. @this.Trash.Select(ToDto)], [.. @this.Reveal.Select(ToDto)]),
      TurnState: new TurnState(@this.Players[@this.CurrentPlayer].Id, @this.ActivePlayerId, @this.CurrentTurn, @this.Phase.ToString()),
      Log: new LogState(@this.Log),
      Me: new FullPlayerData(playerId, [.. player.Hand.Select(ToDto)], player.Deck.Length, [.. player.Discard.Select(ToDto)], [.. player.Play.Select(ToDto)], [.. player.PrivateReveal.Select(ToDto)], player.Resources, player.ActiveFilter),
      Opponents: [
        .. opps.Select(opp => new PartialPlayerData(opp.Id, opp.Hand.Length, opp.Deck.Length, opp.Discard.Length, opp.Discard.LastOrDefault()?.Card.Id, [.. opp.Play.Select(ToDto)], opp.Resources))
      ]
    );
  }

  private static CardInstanceDto ToDto(this CardInstance @this) => new CardInstanceDto(@this.Id, @this.Card.Id);

}
using System.Collections.Immutable;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Services;
using static Dominion.Backend.MoveCardsEffectHandler;

namespace Dominion.Backend;

public record GameState(string GameId, bool GameStarted, CardPileState[] KingdomCards, PlayerState[] Players, CardInstance[] Trash, int CurrentTurn, int CurrentPlayer, int ActivePlayer, Phase Phase, string[] Log, PlayCardResumeState? ResumeState);
public record PlayerState(string Id, CardInstance[] Hand, CardInstance[] Deck, CardInstance[] Discard, CardInstance[] Play, PlayerResources Resources, CardFilter? ActiveFilter);
public record PlayerResources(int Actions, int Buys, int Coins, int Villagers, int Coffers, int Points)
{
  public static readonly PlayerResources Empty = new PlayerResources(0, 0, 0, 0, 0, 0);
  public static PlayerResources EndTurn(PlayerResources current) => current with { Actions = 0, Buys = 0, Coins = 0 };
  public static PlayerResources NewTurn(PlayerResources current) => current with { Actions = 1, Buys = 1, Coins = 0 };
};
public class CardInstance(CardData card)
{
  public static CardInstance CreateByCardId(int cardId) => new CardInstance(MasterCardData.AllCards[cardId]);
  public static CardInstance GetCardInstance(string playerId, string instanceId, CardZone from, GameState state) => from.GetCardZone(playerId, state).Single(card => card.Id == instanceId);
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public CardData Card { get; set; } = card;
}
public record CardPileState(CardData Card, int Remaining);
public record CardData(int Id, string Name, int Cost, int? Value, CardType[] Types, CardEffect[] Effects);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MoveCardsEffect), "move")]
[JsonDerivedType(typeof(SimpleEffect), "simple")]
public abstract class CardEffect { }
public class SimpleEffect : CardEffect { public required string Effect { get; set; } }
public class MoveCardsEffect : CardEffect
{
  public required EffectTarget Target { get; set; }
  public required CardZone To { get; set; }
  public required CardFilter Filter { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonSerializable(typeof(SelectedCardIdsResult))]
[JsonSerializable(typeof(SelectedCardInstancesResult))]
public abstract record CardSelectionResult { }
public record SelectedCardIdsResult(int[] CardIds) : CardSelectionResult;
public record SelectedCardInstancesResult(string[] CardInstanceIds) : CardSelectionResult;

public class CardFilter
{
  public string Id { get; } = Guid.NewGuid().ToString();
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public required CardZone From { get; set; }
  public CardType[]? Types { get; set; }
  public int? MinCost { get; set; }
  public int? MaxCost { get; set; }
  public int? MinCount { get; set; }
  public int? MaxCount { get; set; }
  public int? CardId { get; set; }
}

public enum CardType { Action, Treasure, Victory, Curse, Attack, }
public enum CardZone { Supply, Trash, Deck, Discard, Hand, Play, Exile, }
public enum EffectTarget { Me, All, Opps, }
public enum Phase { Action, BuyOrPlay, Buy, Cleanup }
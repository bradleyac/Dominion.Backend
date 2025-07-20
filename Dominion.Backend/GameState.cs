using System.Text.Json.Serialization;
using Fluent;

namespace Dominion.Backend;

public record GameState(string GameId, bool GameStarted, GameResult? GameResult, CardPileState[] KingdomCards, PlayerState[] Players, CardInstance[] Trash, CardInstance[] Reveal, int CurrentTurn, int CurrentPlayer, string? ActivePlayerId, Phase Phase, string[] Log, PlayCardResumeState? ResumeState, PendingEffect[] EffectStack);
public record PlayerState(string Id, int Index, CardInstance[] Hand, CardInstance[] Deck, CardInstance[] Discard, CardInstance[] Play, CardInstance[] PrivateReveal, PlayerResources Resources, PlayerChoice? ActiveChoice);
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
public record CardData
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required int Cost { get; init; }
  public int Value { get; init; } = 0;
  public Func<GameState, string, int>? ValueFunc { get; init; }
  public required CardType[] Types { get; init; }
  public required FluentEffect[] Effects { get; init; }
};

public record CardFilter
{
  public string Id { get; } = Guid.NewGuid().ToString();
  public required CardZone From { get; init; }
  public CardType[]? Types { get; init; }
  public int? MinCost { get; init; }
  public int? MaxCost { get; init; }
  public int? MinCount { get; init; }
  public int? MaxCount { get; init; }
  public int? CardId { get; init; }
  public int? NotId { get; init; }

  [JsonIgnore]
  public int ExactCount
  {
    init => MinCount = MaxCount = value;
  }

  [JsonIgnore]
  public int ExactCost
  {
    init => MinCost = MaxCost = value;
  }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardType { Action, Treasure, Victory, Curse, Attack, Reaction }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardZone { Supply, Trash, Deck, Discard, Hand, Play, Exile, Reveal, PrivateReveal }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectTarget { All, Opps, }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Phase { Action, BuyOrPlay, Buy, Cleanup }
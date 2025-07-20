using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Fluent;

namespace Dominion.Backend;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(PlayerSelectChoice), "select")]
[JsonDerivedType(typeof(PlayerArrangeChoice), "arrange")]
[JsonDerivedType(typeof(PlayerCategorizeChoice), "categorize")]
public abstract record PlayerChoice
{
  public string Id { get; init; } = Guid.NewGuid().ToString();
  public bool IsForced { get; init; } = true;
  public required string Prompt { get; init; }
  [JsonIgnore]
  public Func<GameState, PlayerChoice, PlayerChoiceResult, EffectContext, GameState> OnDecline { get; init; } = (state, choice, result, context) => state;
}
public record PlayerSelectChoice : PlayerChoice
{
  public required CardFilter Filter { get; init; }

  // TODO: Validation
}

public record PlayerArrangeChoice : PlayerChoice
{
  public required CardZone ZoneToArrange { get; init; }
}

public record PlayerCategorizeChoice : PlayerChoice
{
  public required string DefaultCategory { get; init; }
  public required string[] Categories { get; init; }
  public required CardZone ZoneToCategorize { get; init; }
}

public abstract record PlayerChoiceResult
{
  public bool IsDeclined { get; init; }
}

public record PlayerSelectChoiceResult : PlayerChoiceResult
{
  public required CardInstance[] SelectedCards { get; init; }
}

public record PlayerArrangeChoiceResult : PlayerChoiceResult
{
  public required CardInstance[] ArrangedCards { get; init; }
}

public record PlayerCategorizeChoiceResult : PlayerChoiceResult
{
  public required Dictionary<string, CardInstance[]> CategorizedCards { get; init; }
}
namespace Dominion.Backend;

public static class CardInstanceExtensions
{
  public static IEnumerable<int> CardIds(this IEnumerable<CardInstance> instances) => instances.Select(ci => ci.Card.Id);
}
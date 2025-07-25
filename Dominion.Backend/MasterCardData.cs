namespace Dominion.Backend;

using static Fluent.Fluent;
using static CardZone;
using static CardType;
using static Utils;
using Fluent;
using Microsoft.OpenApi.Any;

public static class MasterCardData
{
  public static class CardIDs
  {
    public const int Copper = 5;
    public const int Silver = 6;
    public const int Gold = 7;
    public const int Estate = 8;
    public const int Duchy = 9;
    public const int Province = 10;
    public const int Curse = 11;
    public const int Witch = 15;
    public const int Bandit = 17;
    public const int Vassal = 22;
    public const int Bureaucrat = 23;
    public const int Militia = 26;
    public const int Sentry = 29;
    public const int ThroneRoom = 30;
    public const int Moat = 33;
    public const int Merchant = 34;
    public const int Beggar = 35;
    public const int Trail = 36;
    public const int Clerk = 37;
  }

  public static readonly (int, int)[] StartingPiles = [
    (CardIDs.Copper, 60),
    (CardIDs.Silver, 40),
    (CardIDs.Gold, 30),
    (CardIDs.Estate, 8),
    (CardIDs.Duchy, 8),
    (CardIDs.Province, 8),
    (CardIDs.Curse, 10)
  ];

  public static readonly (int, int)[] StartingDeck = [
    (CardIDs.Copper, 7),
    (CardIDs.Estate, 3),
    // (CardIDs.Trail, 5),
    // (CardIDs.Bandit, 5),
  ];

  public static readonly Dictionary<int, CardData> AllCards = new CardData[] {
    new() { Id = 1, Name = "Village", Cost = 3, Types = [Action], Effects = [Do(DoActivePlayer(p => p.GainActions(2).DrawCards(1)))]},
    new() { Id = 2, Name = "Laboratory", Cost = 5, Types = [Action], Effects = [Do(DoActivePlayer(p => p.GainActions(1).DrawCards(2)))]},
    new() { Id = 3, Name = "Festival", Cost = 5, Types = [Action ], Effects = [Do(DoActivePlayer(p => p.GainActions(2).GainBuys(1).GainCoins(2)))]},
    new() { Id = 4, Name = "Market", Cost = 5, Types = [Action], Effects = [Do(DoActivePlayer(p => p.GainActions(1).DrawCards(1).GainBuys(1).GainCoins(1)))]},
    new() { Id = 5, Name = "Copper", Cost = 0, Types = [Treasure], Effects = [Do(DoActivePlayer(p => p.GainCoins(1)))]},
    new() { Id = 6, Name = "Silver", Cost = 3, Types = [Treasure], Effects=[Do(DoActivePlayer(p => p.GainCoins(2)))]},
    new() { Id = 7, Name = "Gold", Cost = 6, Types = [Treasure], Effects=[Do(DoActivePlayer(p => p.GainCoins(3)))]},
    new() { Id = 8, Name = "Estate", Cost = 2, Value = 1, Types = [Victory], Effects = [] },
    new() { Id = 9, Name = "Duchy", Cost = 5, Value = 3, Types = [Victory], Effects = [] },
    new() { Id = 10, Name = "Province", Cost = 8, Value = 6, Types = [Victory], Effects = []},
    new() { Id = 11, Name = "Curse", Cost = 0, Value = -1, Types = [Curse], Effects = []},
    new() { Id = 12, Name = "Cellar", Cost = 1, Types = [Action], Effects = [
      Do(DoActivePlayer(p => p.GainActions(1)))
      .ThenSelect((_,_) => new PlayerSelectChoice { Filter = new CardFilter { From = Hand }, Prompt="Select cards to discard" })
      .MoveSelectedCardsTo(Discard)
      .ThenAfterSelect((state, choice, result, ctx) => state.UpdatePlayer(ctx.PlayerId, player => player.DrawCards(result.SelectedCards.Length)))]},
    new() { Id = 13, Name = "Artisan", Cost = 6, Types = [Action], Effects = [
      SelectCards((_,_) => new() { Filter = new CardFilter { From = Supply, MaxCost = 5, ExactCount = 1 }, Prompt = "Select a card to gain to your hand" })
      .GainSelectedCards(to: Hand)
      .ThenSelect((_,_) => new PlayerSelectChoice { Filter = new CardFilter { From = Hand, ExactCount = 1 }, Prompt = "Select a card to put on top of your deck" })
      .MoveSelectedCardsTo(Deck)]},
    new() { Id = 14, Name = "Chapel", Cost = 2, Types = [Action], Effects = [
      SelectCards((_,_) => new() { Filter=new CardFilter { From = Hand, MaxCount = 4 }, Prompt = "Select cards to trash" })
      .MoveSelectedCardsTo(Trash)]},
    new() { Id = 15, Name = "Witch", Cost = 5, Types = [Action, Attack], Effects = [
      Do(DoActivePlayer(p => p.DrawCards(2))),
      ForEach(EffectTarget.Opps, Do((state, ctx) => state.GainCardFromSupply(CardIDs.Curse)))]},
    new() { Id = 16, Name = "Workshop", Cost = 3, Types = [Action], Effects = [
      SelectCards((_,_) => new() { Filter = new CardFilter { From = Supply, MaxCost = 4, ExactCount = 1 }, Prompt = "Select a card to gain" })
      .GainSelectedCards()]},
    new (){ Id = 17, Name = "Bandit", Cost = 5, Types = [Action, Attack], Effects = [
      Do((state, ctx) => state.GainCardFromSupply(CardIDs.Gold)),
        ForEach(EffectTarget.Opps,
          Do(RevealTopN(2))
          .ThenSelect((_,_) => new PlayerSelectChoice { Filter = new CardFilter{ From = Reveal, NotId = CardIDs.Copper, Types = [Treasure], ExactCount = 1 }, Prompt = "Select a treasure to trash" })
          .MoveSelectedCardsTo(Trash)
          .MoveRevealedCardsTo(Discard))]},
    new () { Id = 18, Name = "Forge", Cost = 7, Types = [Action], Effects = [
      SelectCards((_,_) => new() { Filter = new CardFilter { From = Hand }, Prompt = "Select cards to trash" })
      .MoveSelectedCardsTo(Trash)
      .ThenSelect((state, filter, result, ctx) => new()
      {
        Filter = new CardFilter
        {
          From = Supply,
          ExactCount = 1,
          ExactCost = ((PlayerSelectChoiceResult)result).SelectedCards.Sum(card => card.Card.Cost),
        },
        Prompt = "Select a card to gain",
      }).MoveSelectedCardsTo(Discard)] },
    new () { Id = 19, Name = "Mine", Cost = 5, Types = [Action], Effects = [
      SelectCards((_,_) => new() { Filter = new CardFilter { From = Hand, Types = [Treasure], ExactCount = 1 }, Prompt = "Select a treasure to upgrade", IsForced = false })
      .MoveSelectedCardsTo(Trash)
      .ThenSelect((state, filter, result, ctx) => new()
      {
        Filter = new CardFilter
        {
          From = Supply,
          ExactCount = 1,
          MinCost = 0,
          MaxCost = ((PlayerSelectChoiceResult)result).SelectedCards.Single().Card.Cost + 3,
          Types = [Treasure]
        },
        Prompt = "Select a treasure to gain to your hand"
      }).MoveSelectedCardsTo(Hand)]},
    new () { Id = 20, Name = "Remodel", Cost = 4, Types = [Action], Effects = [
      SelectCards((_,_) => new() { Filter = new CardFilter { From = Hand, ExactCount = 1 }, Prompt = "Select a card to trash" })
      .MoveSelectedCardsTo(Trash)
      .ThenSelect((state, filter, result, ctx) => new()
      {
        Filter = new CardFilter
        {
          From = Supply,
          ExactCount = 1,
          MinCost = 0,
          MaxCost = ((PlayerSelectChoiceResult)result).SelectedCards.Single().Card.Cost + 2,
        },
        Prompt = "Select a card to gain"
      }).MoveSelectedCardsTo(Discard)]},
    new () { Id = 21, Name = "Smithy", Cost = 4, Types = [Action], Effects = [Do(DoActivePlayer(p => p.DrawCards(3)))]},
    new () { Id = 22, Name = "Vassal", Cost = 3, Types = [Action], Effects = [
      Do(DoActivePlayer(p => p.GainCoins(2)))
      .Then((state, ctx) => {
        var topCard = state.GetPlayer(ctx.PlayerId).Deck.FirstOrDefault();
        if (topCard != null)
        {
          if (topCard.Card.Types.Contains(Action)) {
            return state.RevealCardsFromDeck(ctx.PlayerId, 1);
          }
          else {
            return state.DiscardCardsFromDeck(ctx.PlayerId, 1);
          }
        }
        return state;
      }).ThenSelect((_,_) => new PlayerSelectChoice { Filter = new CardFilter { From = Reveal, Types = [Action], ExactCount = 1 }, Prompt = "Select an action to play", IsForced = false,
        OnDecline = (state, choice, result, ctx) => state.MoveAllFromZone(Reveal, Discard, ctx.PlayerId)
      }).ThenIfAnySelected((state, choice, result, ctx) => GameLogic.PlayCard(state, ctx.PlayerId, result.SelectedCards[0].Id, Reveal, ignoreCostsAndPhases: true, afterCurrentEffect: true).Item1)]},
    new () { Id = 23, Name = "Bureaucrat", Cost = 4, Types = [Action, Attack], Effects = [
      Do((state, ctx) => state.GainCardFromSupply(CardIDs.Silver, to: Deck)),
      ForEach(EffectTarget.Opps,
        SelectCards((_,_) => new PlayerSelectChoice{ Filter = new CardFilter { From = Hand, Types = [Victory], ExactCount = 1 }, Prompt = "Select a Victory card to put on top of your deck" })
        .ThenIfAnySelected((state, choice, result, ctx) => state.MoveBetweenZones(choice.Filter.From, Deck, ctx.PlayerId, result.SelectedCards)))]},
    new () { Id = 24, Name = "Council Room", Cost = 5, Types = [Action], Effects = [
      Do(DoActivePlayer(p => p.DrawCards(4).GainBuys(1))),
      ForEach(EffectTarget.Opps, Do(DoActivePlayer(p => p.DrawCards(1))))]},
    new () { Id = 25, Name = "Harbinger", Cost = 3, Types = [Action], Effects = [
      Do(DoActivePlayer(p => p.DrawCards(1).GainActions(1)))
      .Then((state, ctx) => state.MoveAllFromZone(Discard, PrivateReveal, ctx.PlayerId))
      .ThenSelect((_, _) => new PlayerSelectChoice { Filter = new CardFilter { From = PrivateReveal, ExactCount = 1 }, Prompt = "Select a card to put on top of your deck", IsForced = false,
        OnDecline = (state, choice, result, ctx) => state.MoveAllFromZone(PrivateReveal, Discard, ctx.PlayerId) })
      .MoveSelectedCardsTo(Deck)
      .MoveRevealedCardsTo(Discard, true)]},
    new () { Id = 26, Name = "Militia", Cost = 4, Types = [Action, Attack], Effects = [
      Do(DoActivePlayer(p => p.GainCoins(2))),
      ForEach(EffectTarget.Opps, SelectCards((state, ctx) => new PlayerSelectChoice { Filter = new CardFilter { From = Hand, ExactCount = Math.Max(0, state.GetPlayer(ctx.PlayerId).Hand.Length - 3) }, Prompt = "Discard down to 3 cards in hand" })
        .MoveSelectedCardsTo(Discard))]},
    new () { Id = 27, Name = "Moneylender", Cost = 4, Types = [Action], Effects = [
      SelectCards((_,_) => new PlayerSelectChoice { Filter = new CardFilter { From = Hand, CardId = CardIDs.Copper, ExactCount = 1 }, Prompt = "Select a Copper to trash", IsForced = false })
      .MoveSelectedCardsTo(Trash)
      .ThenIfAnySelected(ThenActivePlayer(p => p.GainCoins(3)))]},
    new () { Id = 28, Name = "Poacher", Cost = 4, Types = [Action], Effects = [
      Do(DoActivePlayer(p => p.DrawCards(1).GainActions(1).GainCoins(1))),
      SelectCards((state, ctx) =>
      {
        var player = state.GetPlayer(ctx.PlayerId);
        int discardCount = Math.Min(player.Hand.Length, state.KingdomCards.Count(pile => pile.Remaining == 0));
        return new PlayerSelectChoice { Filter = new CardFilter { From = Hand, ExactCount = discardCount }, Prompt = $"Discard {discardCount} card{(discardCount == 1 ? "" : "s")}" };
      }).MoveSelectedCardsTo(Discard)]},
    new () { Id = 29, Name = "Sentry", Cost = 5, Types = [Action], Effects = [
      Do(DoActivePlayer(p => p.DrawCards(1).GainActions(1))),
      Do(RevealTopN(2, privateReveal: true))
      .ThenCategorize((state, ctx) => new PlayerCategorizeChoice { ZoneToCategorize = PrivateReveal, Categories = ["Put Back", "Trash", "Discard"], DefaultCategory="Put Back", Prompt = "Choose cards to trash, discard, or put back" })
      .Then((state, choice, result, ctx) => {
        var actualResult = (PlayerCategorizeChoiceResult)result;
        var actualChoice = (PlayerCategorizeChoice)choice;

        return actualResult.CategorizedCards.Aggregate(state, (stateAccumulator, next) => next.Key == "Put Back" ? stateAccumulator : stateAccumulator.MoveBetweenZones(actualChoice.ZoneToCategorize, findTargetZone(next.Key), ctx.PlayerId, next.Value));

        CardZone findTargetZone(string category) => category switch {
          "Trash" => Trash,
          "Discard" => Discard,
          _ => throw new NotImplementedException()
        };
      })
      .ThenArrange((state, choice, result, ctx) => new() { ZoneToArrange = PrivateReveal, Prompt = "Choose the order in which to put back the cards" })
      .Then((state, choice, result, ctx) => state.MoveBetweenZones(((PlayerArrangeChoice)choice).ZoneToArrange, Deck, ctx.PlayerId, ((PlayerArrangeChoiceResult)result).ArrangedCards))]},
    new () { Id = 30, Name = "Throne Room", Cost = 4, Types = [Action], Effects = [
      SelectCards((_,_) => new PlayerSelectChoice { Filter = new CardFilter { Types = [Action], From = Hand, ExactCount = 1 }, IsForced = false, Prompt = "Select an Action to play twice" })
      .ThenIfAnySelected((state, choice, result, ctx) => GameLogic.PlayCard(state, ctx.PlayerId, result.SelectedCards[0].Id, Hand, ignoreCostsAndPhases: true, count: 2, afterCurrentEffect: true).Item1)]},
    new () { Id = 31, Name = "Gardens", Cost = 4, ValueFunc = (state, playerId) => state.GetPlayer(playerId).Deck.Length / 10, Types = [Victory], Effects = []},
    new () { Id = 32, Name = "Library", Cost = 5, Types = [Action], Effects = [
      Loop((state, ctx) => state.GetPlayer(ctx.PlayerId) is var player && player.Hand.Length < 7 && (player.Deck.Length + player.Discard.Length > 0),
        Do(RevealTopN(1, true))
        .ThenSelect((state, ctx) => new PlayerSelectChoice { Filter = new CardFilter{ From = PrivateReveal, Types = [Action], ExactCount = 1 }, Prompt = "Select an action to set aside", IsForced = false })
        .MoveSelectedCardsTo(Reveal)
        .MoveRevealedCardsTo(Hand, true)),
      Do((state, ctx) => state.MoveAllFromZone(Reveal, Discard, ctx.PlayerId))]},
    new () { Id = 33, Name = "Moat", Cost = 2, Types = [Action, Reaction], Effects = [Do(DoActivePlayer(p => p.DrawCards(2)))],
      CanReact = (ctx) => ctx.Trigger == ReactionTrigger.Play && ctx.TriggerCard!.Card.Types.Contains(Attack) && ctx.ReactingPlayerId != ctx.TriggerOwnerId,
      ReactionIsIdempotent = true,
      ReactionPrompt = "Reveal Moat",
      ReactionEffects = [new EffectSequence((state, triggeringEffectId, triggeredCardInstanceId, triggeredCardLocation, ctx) => state.UpdatePlayer(ctx.PlayerId, p => p with { ImmuneToEffectIds = [.. p.ImmuneToEffectIds, triggeringEffectId] }))]},
    new () { Id = 34, Name = "Merchant", Cost = 3, Types = [Action], Effects = [
      Do(DoActivePlayer(p => p.DrawCards(1).GainActions(1))),
      // TODO: THIS IS WRONG! Should be based on actual count of played silvers and not based on # of silvers in play,
      // you could play the same silver twice and this should only give one. This may not have any practical effect, however, since the trigger is consumed on use.
      // If you somehow played another merchant between two plays of the same silver, then this would be broken.
      Do(DoActivePlayer(p => p with { AmbientTriggers = [.. p.AmbientTriggers, new AmbientTrigger
        {
          CanTrigger = (state, ctx) => ctx.Trigger == ReactionTrigger.Play && ctx.TriggerCard.Card.Id == CardIDs.Silver && state.GetPlayer(ctx.ReactingPlayerId).Play.Count(c => c.Card.Id == CardIDs.Silver) == 1,
          Effects = [Do(DoActivePlayer(p => p.GainCoins(1)))]
        } ]}))] },
    new () { Id = 35, Name = "Beggar", Cost = 2, Types = [Action, Reaction], Effects = [Do((state, ctx) => state.GainCardsFromSupply([CardIDs.Copper, CardIDs.Copper, CardIDs.Copper], to: Hand))],
      CanReact = (ctx) => ctx.Trigger == ReactionTrigger.Play && ctx.TriggerCard!.Card.Types.Contains(Attack) && ctx.ReactingPlayerId != ctx.TriggerOwnerId,
      ReactionPrompt = "Discard Beggar",
      ReactionEffects = [new EffectSequence((state, triggeringEffectId, triggeredCardInstanceId, triggeredCardLocation, ctx) => state
        .GainCardFromSupply(CardIDs.Silver, to: Deck)
        .GainCardFromSupply(CardIDs.Silver)
        .MoveBetweenZones(Hand, Discard, ctx.PlayerId, [CardInstance.GetCardInstance(ctx.PlayerId, triggeredCardInstanceId, Hand, state)]))]},
    new () { Id = 36, Name = "Trail", Cost = 4, Types = [Action, Reaction], Effects = [Do(DoActivePlayer(p => p.DrawCards(1).GainActions(1)))],
      CanReact = (ctx) => ctx.Trigger is ReactionTrigger.Discard or ReactionTrigger.Trash or ReactionTrigger.Gain && ctx.TriggerOwnerId == ctx.ReactingPlayerId && ctx.ReactingCardId == ctx.TriggerCard.Id,
      ReactionPrompt = "Play Trail",
      ReactionEffects = [new EffectSequence((state, triggeringEffectId, triggeredCardInstanceId, triggeredCardLocation, ctx) =>
        GameLogic.PlayCard(state, ctx.PlayerId, triggeredCardInstanceId, triggeredCardLocation, ignoreCostsAndPhases: true, afterCurrentEffect: true).Item1)]},
    new () { Id = 37, Name = "Clerk", Cost = 4, Types = [Action, Reaction, Attack], Effects = [
      Do(DoActivePlayer(p => p.GainCoins(2))),
      ForEach(EffectTarget.Opps, SelectCards((state, ctx) => new PlayerSelectChoice { Filter = new CardFilter { From = Hand, ExactCount = state.GetPlayer(ctx.PlayerId).Hand.Length >= 5 ? 1 : 0 }, Prompt = "Select a card to put onto your deck" })
        .MoveSelectedCardsTo(Deck))],
      CanReact = (ctx) => ctx.Trigger == ReactionTrigger.StartOfTurn,
      ReactionPrompt = "Play Clerk",
      ReactionEffects = [new EffectSequence((state, triggeringEffetId, triggeredCardInstanceId, triggeredCardLocation, ctx) =>
        GameLogic.PlayCard(state, ctx.PlayerId, triggeredCardInstanceId, triggeredCardLocation, ignoreCostsAndPhases: true, afterCurrentEffect: true).Item1)]},
  }.ToDictionary(card => card.Id, card => card);
}
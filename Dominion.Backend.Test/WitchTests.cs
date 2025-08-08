namespace Dominion.Backend.Test;

public class WitchTests
{
  [Fact]
  public void Witch_DrawsTwoCardsAndGivesCurseToOpponent()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameFactory.AddPlayer(gameState, "player2");
    gameState = GameLogic.StartGame(gameState);
    var player1 = gameState.Players[0].Id;
    var player2 = gameState.Players[1].Id;
    var witch = CardInstance.CreateByCardId(15); // Witch
                                                 // Add Witch to player1's hand, ensure player1 has 1 action
    gameState = gameState.UpdatePlayer(player1, p => p with
    {
      Hand = [witch],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Witch
    var (afterPlay, _) = GameLogic.PlayCard(gameState, player1, witch.Id, Hand);
    var updatedPlayer1 = afterPlay.GetPlayer(player1);
    var updatedPlayer2 = afterPlay.GetPlayer(player2);
    // Player1 should have drawn 2 cards (hand size 2 higher, minus Witch played)
    Assert.Equal(2, updatedPlayer1.Hand.Length); // 1 start -1 played +2 drawn
    Assert.Equivalent(updatedPlayer1.Play, new[] { witch }); // Witch in play
                                                             // Player2 should have a Curse in discard
    Assert.Contains(updatedPlayer2.Discard, c => c.Card.Id == 11); // Curse
  }

  [Fact]
  public void Witch_DoesNotGiveCurseIfSupplyEmpty()
  {
    var gameState = GameFactory.CreateGameState("player1");
    gameState = GameFactory.AddPlayer(gameState, "player2");
    gameState = GameLogic.StartGame(gameState);
    var player1 = gameState.Players[0].Id;
    var player2 = gameState.Players[1].Id;
    var witch = CardInstance.CreateByCardId(15); // Witch
                                                 // Remove all Curses from supply
    gameState = gameState with
    {
      KingdomCards = gameState.KingdomCards.Select(pile => pile.Card.Id == 11 ? pile with { Remaining = 0 } : pile).ToArray()
    };
    // Add Witch to player1's hand
    gameState = gameState.UpdatePlayer(player1, p => p with
    {
      Hand = [witch],
      Resources = p.Resources with { Actions = 1 }
    });
    // Play Witch
    var (afterPlay, _) = GameLogic.PlayCard(gameState, player1, witch.Id, Hand);
    var updatedPlayer2 = afterPlay.GetPlayer(player2);
    // Player2 should not have a Curse in discard
    Assert.DoesNotContain(updatedPlayer2.Discard, c => c.Card.Id == 11);
  }
}
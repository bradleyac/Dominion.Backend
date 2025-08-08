public class VictoryAndCurseTests
{
  [Fact]
  public void Estate_Score_Is_Correct()
  {
    var gameState = GameFactory.CreateGameState("player1");
    var playerId = gameState.Players[0].Id;
    // Give player 5 Estates
    var estates = Enumerable.Range(0, 5).Select(_ => CardInstance.CreateByCardId(8)).ToArray();
    gameState = gameState.UpdatePlayer(playerId, p => p with { Deck = estates });

    // End the game
    gameState = SetUpGameEnd(gameState);
    gameState = GameLogic.EndTurn(gameState);

    var score = gameState.GameResult!.Scores[playerId];
    Assert.Equal(5, score); // 5 Estates * 1 point each
  }

  [Fact]
  public void Duchy_Score_Is_Correct()
  {
    var gameState = GameFactory.CreateGameState("player1");
    var playerId = gameState.Players[0].Id;
    // Give player 2 Duchies
    var duchies = Enumerable.Range(0, 2).Select(_ => CardInstance.CreateByCardId(9)).ToArray();
    gameState = gameState.UpdatePlayer(playerId, p => p with { Deck = duchies });

    gameState = SetUpGameEnd(gameState);
    gameState = GameLogic.EndTurn(gameState);

    var score = gameState.GameResult!.Scores[playerId];
    Assert.Equal(6, score); // 2 Duchies * 3 points each
  }

  [Fact]
  public void Province_Score_Is_Correct()
  {
    var gameState = GameFactory.CreateGameState("player1");
    var playerId = gameState.Players[0].Id;
    // Give player 1 Province
    var provinces = new[] { CardInstance.CreateByCardId(10) };
    gameState = gameState.UpdatePlayer(playerId, p => p with { Deck = provinces });

    gameState = SetUpGameEnd(gameState);
    gameState = GameLogic.EndTurn(gameState);

    var score = gameState.GameResult!.Scores[playerId];
    Assert.Equal(6, score); // 1 Province * 6 points
  }

  [Fact]
  public void Curse_Score_Is_Correct()
  {
    var gameState = GameFactory.CreateGameState("player1");
    var playerId = gameState.Players[0].Id;
    // Give player 4 Curses
    var curses = Enumerable.Range(0, 4).Select(_ => CardInstance.CreateByCardId(11)).ToArray();
    gameState = gameState.UpdatePlayer(playerId, p => p with { Deck = curses });

    gameState = SetUpGameEnd(gameState);
    gameState = GameLogic.EndTurn(gameState);

    var score = gameState.GameResult!.Scores[playerId];
    Assert.Equal(-4, score); // 4 Curses * -1 point each
  }

  [Fact]
  public void Mixed_Victory_And_Curse_Score_Is_Correct()
  {
    var gameState = GameFactory.CreateGameState("player1");
    var playerId = gameState.Players[0].Id;
    // 2 Estates, 1 Duchy, 1 Province, 3 Curses
    var cards = new[]
    {
            CardInstance.CreateByCardId(8), CardInstance.CreateByCardId(8), // Estates
            CardInstance.CreateByCardId(9), // Duchy
            CardInstance.CreateByCardId(10), // Province
            CardInstance.CreateByCardId(11), CardInstance.CreateByCardId(11), CardInstance.CreateByCardId(11) // Curses
        };
    gameState = gameState.UpdatePlayer(playerId, p => p with { Deck = cards });

    gameState = SetUpGameEnd(gameState);
    gameState = GameLogic.EndTurn(gameState);

    var score = gameState.GameResult!.Scores[playerId];
    // 2*1 + 1*3 + 1*6 + 3*-1 = 2 + 3 + 6 - 3 = 8
    Assert.Equal(8, score);
  }

  private static GameState SetUpGameEnd(GameState state)
  {
    var kingdomCards = state.KingdomCards
    .Select(pile => pile.Card.Id == 10 // Province
        ? pile with { Remaining = 0 }
        : pile)
    .ToArray();
    return state with { KingdomCards = kingdomCards };
  }
}
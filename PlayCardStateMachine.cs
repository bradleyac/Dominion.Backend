// using System.Runtime.CompilerServices;
// using Stateless;

// namespace Dominion.Backend;

// public class PlayCardStateMachine
// {
//   private enum State { Ready, Resolving, WaitingForSelection }
//   private enum Trigger { Start, SelectionMade }
//   private StateMachine<State, Trigger> _machine;
//   private StateMachine<State, Trigger>.TriggerWithParameters<ChosenCards> _selectionMadeTrigger;
//   private List<EffectDef> _effectQueue;
//   private EffectExecutor _executor;
//   private CardInstance _cardInstance;

//   public PlayCardStateMachine(CardInstance cardInstance, string playerId, GameState gameState)
//   {
//     _machine = new StateMachine<State, Trigger>(State.Ready);
//     _cardInstance = cardInstance;
//   }

//   private void ConfigureStateMachine()
//   {
//     _machine.Configure(State.Ready)
//       .Permit(Trigger.Start, State.Resolving);

//     _machine.Configure(State.Resolving)
//       .OnEntry(ResolveEffects);

//     _machine.Configure(State.WaitingForSelection)
//       .Permit(Trigger.SelectionMade, State.Resolving);
//   }

//   private void ResolveEffects()
//   {
//     foreach (var effect in _effectQueue)
//     {
//       _executor.RequiresSelection(effect, 
//     }
//   }
// }
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class BattleManagerGenerator
{
    public BattleManager Create(ILogProvider log, IRandomProvider random, IInputProvider input, 
        IReadOnlyList<EnemyCharacter> enemies, PartyController partyController, FieldType fieldType, int floorNum = 0)
    {
        ProvidorContext providers = new (log, random, input);
        GameSelectionService selections = GetSelections(log, input);
        BattleServices battleServices = GetBattleServices(random, log, selections);
        BattleSession session = new(partyController.PartyMember, enemies);
        BattleRuntimeContext battleRuntimecontext = new();
        FieldContext fieldContext = new(fieldType, floorNum);
        return new BattleManager
            (providers, battleServices, battleRuntimecontext, partyController,session, fieldContext);
    }

    private GameSelectionService GetSelections(ILogProvider log, IInputProvider input)
    {
        CommandSelect commandSelect = new(log, input);
        TargetSelect targetSelect = new(log, input);
        TargetResolver targetResolver = new();
        SkillSelection skillSelection = new(log, input);
        UseItemSelecter useItemSelecter = new(log, input);
        return new GameSelectionService(commandSelect, targetSelect, targetResolver, skillSelection, useItemSelecter);
    }

    private BattleServices GetBattleServices(IRandomProvider random, ILogProvider log, GameSelectionService selectionService)
    {
        BattleCalculator battleCalculator = new(random);
        TurnScheduler turnScheduler = new(random);
        BattleRewardCalculator battleRewardCalculator = new(random);
        ActionExecutor actionExecutor = new(battleCalculator, log);
        BattleActionQueue battleActionQueue = new(selectionService);
        return new BattleServices(turnScheduler, battleRewardCalculator, battleActionQueue, actionExecutor);
    }

}

public record ProvidorContext
(
    ILogProvider LogProvider,
    IRandomProvider RandomProvider,
    IInputProvider InputProvider
);

public record BattleServices
(
    TurnScheduler TurnScheduler,
    BattleRewardCalculator BattleRewardCalculator,
    BattleActionQueue BattleActionQueue,
    ActionExecutor ActionExecutor
);

public class BattleRuntimeContext()
{
    public Queue<ActionUnit[]> ActionQueue { get; private set; } = new(); //基本キュー
    public Queue<ActionUnit[]> InterruptAction { get; } = new(); //割り込み待機キュー
    public List<(ActionUnit, int)> SubStackInterrupt { get; } = new(); //割り込み待機キューに積むための一時スタック

    public void Enqueue(Queue<ActionUnit[]> actionUnits)
    {
        ActionQueue.Clear();

        while (actionUnits.Count > 0)
        {
            ActionQueue.Enqueue(actionUnits.Dequeue());
        }
    }

    public void EnqueueInterrupt(ActionUnit[] interruptAction)
    {
        InterruptAction.Enqueue(interruptAction);
    }
    public void StackAction((ActionUnit, int) stackAction)
    {
        SubStackInterrupt.Add(stackAction);
    }
    public bool IsActionEmpty()
    {
        return (ActionQueue.Count == 0 && InterruptAction.Count == 0);
    }
    public bool TryGetNextAction(out ActionUnit[] nextAction)
    {
        ReleaseStack();
        if(InterruptAction.Count > 0)
        {
            Console.WriteLine("割り込みアクションを取得");
            nextAction = InterruptAction.Dequeue();
            return true;
        }
        else if (ActionQueue.Count > 0)
        {
            nextAction = ActionQueue.Dequeue();
            return true;
        }

        nextAction = null;
        return false;
    }

    private void ReleaseStack()
    {
        if(SubStackInterrupt.Count == 0)
        {
            return;
        }
        var stacks = SubStackInterrupt.
            GroupBy(x => x.Item2, x => x.Item1).Select(group => group.ToArray()).ToList();

        foreach(var stack in stacks)
        {
            EnqueueInterrupt(stack);
        }
        SubStackInterrupt.Clear();
    }

}


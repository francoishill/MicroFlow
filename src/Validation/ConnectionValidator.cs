using System.Collections.Generic;
using System.Linq;

namespace MicroFlow
{
  public sealed class ConnectionValidator : FlowValidator
  {
    protected override void PerformGlobalValidation(FlowDescription flowDescription)
    {
      if (flowDescription.InitialNode == null)
      {
        Result.AddError("Initial node isn't set");
      }
    }

    protected override void VisitActivity<TActivity>(ActivityNode<TActivity> activityNode)
    {
      CheckActivityNode(activityNode);
    }

    protected override void VisitSwitch<TChoice>(SwitchNode<TChoice> switchNode)
    {
      if (switchNode.Choice == null)
      {
        Result.AddError(switchNode, "Choice expression isn't set");
      }

      if (!switchNode.Cases.Any() && switchNode.DefaultCase == null)
      {
        Result.AddError(switchNode, "Switch contains no cases");
      }

      CheckSelfReference(switchNode, switchNode.DefaultCase);

      var choices = new List<TChoice>();
      bool hasDuplicates = false;

      foreach (KeyValuePair<TChoice, IFlowNode> choiceToNode in switchNode.Cases)
      {
        CheckSelfReference(switchNode, choiceToNode.Value);

        if (choices.Contains(choiceToNode.Key))
        {
          hasDuplicates = true;
        }

        choices.Add(choiceToNode.Key);
      }

      if (hasDuplicates)
      {
        Result.AddError(switchNode, "Switch has duplicate cases");
      }
    }

    protected override void VisitCondition(ConditionNode conditionNode)
    {
      if (conditionNode.Condition == null)
      {
        Result.AddError(conditionNode, "Condition isn't set");
      }

      if (conditionNode.WhenTrue == null)
      {
        Result.AddError(conditionNode, "True branch isn't set");
      }

      CheckSelfReference(conditionNode, conditionNode.WhenFalse);
      CheckSelfReference(conditionNode, conditionNode.WhenTrue);
    }

    protected override void VisitForkJoin(ForkJoinNode forkJoinNode)
    {
      if (forkJoinNode.Forks.Count == 0)
      {
        Result.AddError(forkJoinNode, "Node has no forks");
      }

      CheckActivityNode(forkJoinNode);
    }

    protected override void VisitBlock(BlockNode blockNode)
    {
      CheckSelfReference(blockNode, blockNode.PointsTo);

      foreach (IFlowNode innerNode in blockNode.InnerNodes)
      {
        CheckSelfReference(blockNode, innerNode);
      }

      var containednessValidator = new BlockSelfContainednessValidator(
        blockNode, Flow.DefaultFaultHandler, Flow.DefaultCancellationHandler);

      ValidationResult isSelfContainedResult = containednessValidator.Validate();

      if (isSelfContainedResult.HasErrors)
      {
        Result.TakeErrorsFrom(isSelfContainedResult);
        return;
      }

      if (!BlockAcyclityChecker.IsAcyclic(blockNode))
      {
        Result.AddError(blockNode, "Block has cycle");
      }
    }

    private void CheckActivityNode(IActivityNode node)
    {
      CheckSelfReference(node, node.PointsTo);
      CheckSelfReference(node, node.FaultHandler);
      CheckSelfReference(node, node.CancellationHandler);

      if (node.FaultHandler == null && Flow.DefaultFaultHandler == null)
      {
        Result.AddError(node, "Neither node nor the flow has a fault handler");
      }

      if (node.CancellationHandler == null && Flow.DefaultCancellationHandler == null)
      {
        Result.AddError(node, "Neither node nor the flow has a cancellation handler");
      }
    }

    private void CheckSelfReference(IFlowNode from, IFlowNode to)
    {
      if (ReferenceEquals(from, to))
      {
        Result.AddError(from, "Node points to itself");
      }
    }
  }
}
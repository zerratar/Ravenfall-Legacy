using System;
using System.Collections.Generic;

namespace Assets.Scripts.Gambit
{
    public interface IGambitRule<TKnowledgeBase>
    {
        string Name { get; }
        bool Process(TKnowledgeBase fact);
    }

    public interface IGambitRuleCondition<TKnowledgeBase>
    {
        bool TestCondition(TKnowledgeBase fact);
    }

    public interface IGambitRuleAction<TKnowledgeBase>
    {
        void Invoke(TKnowledgeBase fact);
    }

    public interface IGambit<TKnowledgeBase>
    {
        void AddRule(IGambitRule<TKnowledgeBase> rule);
        void RemoveRule(IGambitRule<TKnowledgeBase> rule);
        bool ProcessRules(TKnowledgeBase fact);
    }

    public interface IGambitRuleGenerator
    {
        IGambitRuleAction<TKnowledgeBase> CreateAction<TKnowledgeBase>(Action<TKnowledgeBase> onConditionMet);
        IGambitRuleCondition<TKnowledgeBase> CreateCondition<TKnowledgeBase>(Func<TKnowledgeBase, bool> condition);
        IGambitRule<TKnowledgeBase> CreateRule<TKnowledgeBase>(string name, IGambitRuleCondition<TKnowledgeBase> condition, IGambitRuleAction<TKnowledgeBase> action);
    }

    public class Gambit<TKnowledgeBase> : IGambit<TKnowledgeBase>
    {
        private readonly List<IGambitRule<TKnowledgeBase>> rules = new List<IGambitRule<TKnowledgeBase>>();
        private readonly object mutex = new object();

        public bool ProcessRules(TKnowledgeBase fact)
        {
            var anyRulesApplied = false;
            foreach (var rule in rules)
            {
                anyRulesApplied = anyRulesApplied || rule.Process(fact);
            }
            return anyRulesApplied;
        }

        public void AddRule(IGambitRule<TKnowledgeBase> rule)
        {
            lock (mutex) rules.Add(rule);
        }

        public void RemoveRule(IGambitRule<TKnowledgeBase> rule)
        {
            lock (mutex) rules.Remove(rule);
        }
    }

    public class GambitRuleGenerator : IGambitRuleGenerator
    {
        public IGambitRuleAction<TKnowledgeBase> CreateAction<TKnowledgeBase>(Action<TKnowledgeBase> onConditionMet)
        {
            return new GambitRuleAction<TKnowledgeBase>(onConditionMet);
        }

        public IGambitRuleCondition<TKnowledgeBase> CreateCondition<TKnowledgeBase>(Func<TKnowledgeBase, bool> condition)
        {
            return new GambitRuleCondition<TKnowledgeBase>(condition);
        }

        public IGambitRule<TKnowledgeBase> CreateRule<TKnowledgeBase>(
            string name,
            IGambitRuleCondition<TKnowledgeBase> condition,
            IGambitRuleAction<TKnowledgeBase> action)
        {
            return new GambitRule<TKnowledgeBase>(name, condition, action);
        }

        private class GambitRuleCondition<TKnowledgeBase> : IGambitRuleCondition<TKnowledgeBase>
        {
            private Func<TKnowledgeBase, bool> condition;

            public GambitRuleCondition(Func<TKnowledgeBase, bool> condition)
            {
                this.condition = condition;
            }

            public bool TestCondition(TKnowledgeBase fact)
            {
                return condition(fact);
            }
        }

        private class GambitRuleAction<TKnowledgeBase> : IGambitRuleAction<TKnowledgeBase>
        {
            private Action<TKnowledgeBase> onConditionMet;

            public GambitRuleAction(Action<TKnowledgeBase> onConditionMet)
            {
                this.onConditionMet = onConditionMet;
            }

            public void Invoke(TKnowledgeBase fact)
            {
                onConditionMet.Invoke(fact);
            }
        }

        private class GambitRule<TKnowledgeBase> : IGambitRule<TKnowledgeBase>
        {
            private IGambitRuleCondition<TKnowledgeBase> condition;
            private IGambitRuleAction<TKnowledgeBase> action;

            public string Name { get; }

            public GambitRule(
                string name,
                IGambitRuleCondition<TKnowledgeBase> condition,
                IGambitRuleAction<TKnowledgeBase> action)
            {
                this.Name = name;
                this.condition = condition;
                this.action = action;
            }

            public bool Process(TKnowledgeBase fact)
            {
                if (!condition.TestCondition(fact))
                {
                    return false;
                }

                action.Invoke(fact);
                return true;
            }
        }
    }
}

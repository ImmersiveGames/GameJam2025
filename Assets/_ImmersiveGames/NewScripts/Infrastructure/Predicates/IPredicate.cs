using System;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.NewScripts.Infrastructure.Predicates
{
    public interface IPredicate
    {
        bool Evaluate();
    }

    public sealed class And : IPredicate
    {
        private readonly IReadOnlyList<IPredicate> _rules;

        public And(params IPredicate[] rules)
        {
            Preconditions.CheckNotNull(rules, "Rules cannot be null.");
            if (rules.Length == 0)
            {
                throw new ArgumentException("At least one predicate is required.", nameof(rules));
            }
            _rules = rules.ToList().AsReadOnly();
        }

        public bool Evaluate() => _rules.All(r => r.Evaluate());

        public IReadOnlyList<IPredicate> GetRules() => _rules;

        public And AndWith(IPredicate predicate)
        {
            var newRules = _rules.ToList();
            newRules.Add(Preconditions.CheckNotNull(predicate, "Predicate cannot be null."));
            return new And(newRules.ToArray());
        }
    }

    public sealed class Or : IPredicate
    {
        private readonly IReadOnlyList<IPredicate> _rules;

        public Or(params IPredicate[] rules)
        {
            Preconditions.CheckNotNull(rules, "Rules cannot be null.");
            if (rules.Length == 0)
            {
                throw new ArgumentException("At least one predicate is required.", nameof(rules));
            }
            _rules = rules.ToList().AsReadOnly();
        }

        public IReadOnlyList<IPredicate> GetRules() => _rules;

        public bool Evaluate() => _rules.Any(r => r.Evaluate());

        public Or OrWith(IPredicate predicate)
        {
            var newRules = _rules.ToList();
            newRules.Add(Preconditions.CheckNotNull(predicate, "Predicate cannot be null."));
            return new Or(newRules.ToArray());
        }
    }

    public sealed class Not : IPredicate
    {
        private readonly IPredicate _rule;

        public Not(IPredicate rule)
        {
            _rule = Preconditions.CheckNotNull(rule, "Rule cannot be null.");
        }

        public IPredicate GetRule() => _rule;

        public bool Evaluate() => !_rule.Evaluate();
    }
}

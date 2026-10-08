
namespace HolenderGames.StatSystem
{
    /// <summary>
    /// Holds information about a specific stat and its modifiers and upgrades
    /// </summary>
    [System.Serializable]
    public class StatValue
    {
        public float baseValue;
        public float additive;
        public float multiplier;

        public float FinalValue => (baseValue + additive) * multiplier;

        public StatValue(float baseValue)
        {
            this.baseValue = baseValue;
            additive = 0;
            multiplier = 1;
        }

        public void ApplyEffect(StatOperation op, float value)
        {
            switch (op)
            {
                case StatOperation.Add:
                    additive += value;
                    break;

                case StatOperation.Multiply:
                    multiplier *= value;
                    break;
            }
        }

        public float EvaluateEffect(StatOperation op, float value)
        {
            float orig_additive = additive;
            float orig_multiplier = multiplier;

            ApplyEffect(op, value);

            float evaluated_value = FinalValue;

            additive = orig_additive;
            multiplier = orig_multiplier;

            return evaluated_value;
        }
    }

}


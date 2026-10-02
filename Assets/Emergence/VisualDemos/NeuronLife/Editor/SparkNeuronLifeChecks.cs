#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
namespace Emergence.VisualDemos.Editor
{
    public static class SparkNeuronLifeChecks
    {
        public static string Run()
        {
            var type = Type.GetType("Emergence.VisualDemos.SparkNeuronLifeDemo, Assembly-CSharp");
            if (type == null) return "FAIL: dedicated neuron demo component is missing";
            var demo = UnityEngine.Object.FindFirstObjectByType(type);
            if (demo == null) return "FAIL: neuron demo is not in the active scene";
            Func<string, int> number = name => (int)type.GetProperty(name).GetValue(demo);
            if (number("NodeCount") != 8) return "FAIL: expected 8 live neurons";
            int before = number("TotalActivations");
            bool accepted = (bool)type.GetMethod("Stimulate").Invoke(demo, new object[] { 0 });
            if (!accepted || number("TotalActivations") <= before) return "FAIL: stimulation does not activate a neuron";
            if ((bool)type.GetMethod("Stimulate").Invoke(demo, new object[] { -1 })) return "FAIL: invalid index accepted";
            if (number("ActivePulseCount") > 32) return "FAIL: pulse budget exceeded";
            type.GetMethod("SetPaused").Invoke(demo, new object[] { true });
            if ((bool)type.GetMethod("Stimulate").Invoke(demo, new object[] { 1 })) return "FAIL: paused demo accepts stimulation";
            type.GetMethod("SetPaused").Invoke(demo, new object[] { false });
            return "PASS: 8 neurons; stimulation works; invalid index rejected; bounded pulse count; pause blocks stimulation";
        }
    }
}
#endif

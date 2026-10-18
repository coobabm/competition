using System;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop calculator's keypad logic (2026-10-19). Replaces DreamOS's CalculatorManager: same GUID, serialized
    /// fields and button method names. The window itself is reused as the 摆渡众包 workbench.
    ///
    /// A chain calculator, left to right: each operator settles the one before it (2 + 3 × 4 = 20), "=" settles the
    /// last one, and pressing "=" again repeats that step (2 + 3 = = gives 8). DreamOS applied the new operator to the
    /// old pair instead (2 + 3 × 4 = 24); that is fixed here.
    /// </summary>
    public class CalculatorApp : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI displayText;
        [SerializeField] TextMeshProUGUI displayOperator;
        [SerializeField] TextMeshProUGUI displayResult;
        [SerializeField] TextMeshProUGUI displayPreview;

        /// <summary>The running result, once an operator has been pressed.</summary>
        float rememberValue;
        /// <summary>The operator waiting for its right-hand number, and its symbol.</summary>
        Func<float, float, float> pending;
        string pendingSymbol;
        /// <summary>The last right-hand number, for repeated "=".</summary>
        float lastEntry;
        /// <summary>The display holds a result, not a number being typed.</summary>
        bool pressedOperators;
        /// <summary>"=" was the last key: a new number starts a new sum, an operator carries the result on.</summary>
        bool afterEqual;
        bool enableDot = true;
        bool isResetted = true;

        void Awake()
        {
            if (displayText == null || displayResult == null || displayPreview == null)
            {
                Debug.LogError("Calculator - Display resources are not assigned.", this);
                return;
            }
            displayText.text = "0";
            displayResult.text = "0";
            displayPreview.text = "";
        }

        public void ButtonNumber(int number)
        {
            // After an operator the next digit starts a new entry.
            if (pressedOperators)
            {
                displayText.text = number.ToString();
                pressedOperators = false;
                enableDot = true;
            }
            else displayText.text = displayText.text != "0" ? displayText.text + number : number.ToString();
        }

        public void ButtonDot()
        {
            if (!enableDot) return;
            displayText.text += ".";
            enableDot = false;
        }

        public void ButtonDelete()
        {
            if (displayText.text.Length >= 2) displayText.text = displayText.text.Remove(displayText.text.Length - 1);
            else
            {
                displayText.text = "0";
                enableDot = true;
            }
        }

        public void ButtonDivision() => Operate("÷", (a, b) => a / b);

        public void ButtonMultiply() => Operate("×", (a, b) => a * b);

        public void ButtonSubtraction() => Operate("-", (a, b) => a - b);

        public void ButtonAddition() => Operate("+", (a, b) => a + b);

        void Operate(string symbol, Func<float, float, float> apply)
        {
            if (isResetted || afterEqual && !pressedOperators)
            {
                rememberValue = Entry;
                isResetted = false;
            }
            // Two operators in a row (or one right after "=") just change the operator.
            else if (!pressedOperators) rememberValue = pending(rememberValue, Entry);
            afterEqual = false;

            pending = apply;
            pendingSymbol = symbol;
            ShowResult();
            displayPreview.text = Format(rememberValue) + " " + symbol;
            pressedOperators = true;
        }

        public void ButtonEqual()
        {
            if (pending == null || isResetted) return;
            // A fresh number is the right-hand side ("=" after "=" and a new number repeats the step on it);
            // "=" again reuses the last one, and "2 + =" uses the 2.
            if (!pressedOperators)
            {
                if (afterEqual) rememberValue = Entry;
                else lastEntry = Entry;
            }
            else if (!afterEqual) lastEntry = rememberValue;
            rememberValue = pending(rememberValue, lastEntry);
            afterEqual = true;
            ShowResult();
            displayPreview.text = "";
            pressedOperators = true;
        }

        float Entry => float.Parse(displayText.text);

        void ShowResult()
        {
            displayResult.text = Format(rememberValue);
            displayOperator.text = pendingSymbol;
        }

        static string Format(float value) => value.ToString();

        public void ButtonAC()
        {
            displayText.text = "0";
            displayResult.text = "0";
            displayPreview.text = "";
            enableDot = true;
            isResetted = true;
            pending = null;
            pressedOperators = false;
            afterEqual = false;
        }
    }
}

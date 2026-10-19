using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>The game center's tic-tac-toe (replaces DreamOS's TicTacToe sample; same GUID and fields). X starts.</summary>
    public class TicTacToeGame : MonoBehaviour
    {
        static readonly int[][] Lines =
        {
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
            new[] { 0, 4, 8 }, new[] { 2, 4, 6 }
        };

        public TextMeshProUGUI[] cells;
        public ButtonManager[] cellButtons;
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverText;

        string currentPlayer;
        string[] board;
        bool isGameOver;

        void Start()
        {
            currentPlayer = "X";
            board = new string[9];
            isGameOver = false;
            gameOverPanel.SetActive(false);
        }

        /// <summary>Wired to each cell's button with its index (0–8, row by row).</summary>
        public void CellClicked(int index)
        {
            if (isGameOver || board[index] != null) return;
            board[index] = currentPlayer;
            cells[index].text = currentPlayer;
            cellButtons[index].Interactable(false);

            if (System.Array.Exists(Lines, l => board[l[0]] == currentPlayer && board[l[1]] == currentPlayer && board[l[2]] == currentPlayer))
                GameOver(currentPlayer + " wins!");
            else if (System.Array.TrueForAll(board, c => c != null))
                GameOver("Draw!");
            currentPlayer = currentPlayer == "X" ? "O" : "X";
        }

        void GameOver(string message)
        {
            isGameOver = true;
            gameOverPanel.SetActive(true);
            gameOverText.text = message;
        }

        public void RestartGame()
        {
            isGameOver = false;
            gameOverPanel.SetActive(false);
            currentPlayer = "X";
            board = new string[9];
            foreach (var cell in cells) cell.text = "";
            foreach (var button in cellButtons) button.Interactable(true);
        }
    }
}

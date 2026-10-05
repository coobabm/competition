using UnityEngine;
using TMPro;

namespace Michsky.DreamOS.Examples
{
    public class TicTacToe : MonoBehaviour
    {
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

        public void CellClicked(int index)
        {
            if (isGameOver || board[index] != null)
                return;

            board[index] = currentPlayer;
            cells[index].text = currentPlayer;
            cellButtons[index].Interactable(false);

            CheckWinConditions();
            SwitchPlayer();
        }

        void CheckWinConditions()
        {
            // Horizontal win conditions
            for (int i = 0; i <= 6; i += 3)
            {
                if (board[i] == currentPlayer && board[i + 1] == currentPlayer && board[i + 2] == currentPlayer)
                {
                    GameOver(currentPlayer + " wins!");
                    return;
                }
            }

            // Vertical win conditions
            for (int i = 0; i <= 2; i++)
            {
                if (board[i] == currentPlayer && board[i + 3] == currentPlayer && board[i + 6] == currentPlayer)
                {
                    GameOver(currentPlayer + " wins!");
                    return;
                }
            }

            // Diagonal win conditions
            if (board[0] == currentPlayer && board[4] == currentPlayer && board[8] == currentPlayer ||
                board[2] == currentPlayer && board[4] == currentPlayer && board[6] == currentPlayer)
            {
                GameOver(currentPlayer + " wins!");
                return;
            }

            // Check for a draw
            if (IsBoardFull())
            {
                GameOver("Draw!");
                return;
            }
        }

        bool IsBoardFull()
        {
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == null)
                    return false;
            }
            return true;
        }

        void GameOver(string message = "")
        {
            isGameOver = true;
            gameOverPanel.SetActive(true);
            gameOverText.text = message;
        }

        void SwitchPlayer()
        {
            currentPlayer = currentPlayer == "X" ? "O" : "X";
        }

        public void RestartGame()
        {
            isGameOver = false;
            gameOverPanel.SetActive(false);
            currentPlayer = "X";
            board = new string[9];

            for (int i = 0; i < cells.Length; i++) { cells[i].text = ""; }
            for (int i = 0; i < cellButtons.Length; i++) { cellButtons[i].Interactable(true); }
        }
    }
}
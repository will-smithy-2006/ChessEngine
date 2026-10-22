using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Resources;
using ChessEngine.Properties;
using System.Diagnostics;

namespace ChessEngineNS
{
    public partial class MainMenu : Form
    {
        public static readonly string saveFilePath = "SaveFile.txt";

        // Instance of the chess form that will be created and ran.
        private ChessForm chess;

        // Variables to track the currently selected game settings.
        private int selectedDifficulty;
        private bool playingComputer;
        private int playerColour;

        private int randomisedColour;

        // The class constructor initialises the game settings variables, and generates a random colour
        // that will be the player's starting colour is they select the random option.
        public MainMenu()
        {
            InitializeComponent();

            selectedDifficulty = DifficultySlider.Value;
            playingComputer = true;
            playerColour = Piece.White;

            Random random = new Random();
            randomisedColour = random.Next(0, 2) == 0 ? Piece.White : Piece.Black;
        }

        // This subroutine is called when the new game button is clicked. It hides the main menu panel,
        // then shows the new game panel, and moves it to the centre of the screen.
        private void NewGameButton_Click(object sender, EventArgs e)
        {
            MainMenuPanel.Hide();

            NewGamePanel.Location = new Point(139, 44);
            NewGamePanel.Show();
        }

        // This subroutine is called when the exit game button is clicked, and shuts the application
        // and any forms currently open.
        private void ExitButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // This subroutine is called whenever the load game button is clicked, and will read the game's
        // save file and load the game that is saved within this file.
        private void LoadGameButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Open the game save file using a stream reader.
                using (StreamReader sr = new StreamReader(saveFilePath))
                {
                    // The first line of the save file is the fen string of the current position.
                    string fenString = sr.ReadLine();

                    // If this string is empty, then the save file does not contain a board state,
                    // so exit the subroutine.
                    if (fenString == null)
                        return;

                    // The next line is a string that indicates whether the player is facing the 
                    // computer.
                    bool playingComputer = sr.ReadLine() == "true" ? true : false;
                    int difficulty = 5;

                    // If the player is facing the computer, read the next integer stored within 
                    // the file which represents the difficulty setting. Otherwise, the difficulty
                    // will remain as five (so the player can recieve the best hints).
                    if (playingComputer)
                    {
                        difficulty = Convert.ToInt32(sr.ReadLine());
                    }

                    // The next integer stored in the save file is the player's colour.
                    int playerColour = Convert.ToInt32(sr.ReadLine());

                    // Hide this form.
                    Hide();

                    // Assign the chess member variable to a new instance of the Chess Form class
                    // passing in the values stored in the save file. Show this form.
                    chess = new ChessForm(fenString, playingComputer, difficulty, playerColour);
                    chess.ShowDialog();
                }
            }
            // If any errors are caught during this process, display the corresponding error message.
            catch (Exception ex)
            {
                Trace.WriteLine("The save file could not be read.");
                Trace.WriteLine($"Error Message: {ex.Message}");
            }
        }

        // This subroutine updates the selected difficulty setting whenever, the slider is adjusted.
        private void DifficultySlider_Scroll(object sender, EventArgs e)
        {
            selectedDifficulty = DifficultySlider.Value;
        }

        // This subroutine updates the playing computer boolean, and accordingly will hide/show the 
        // game settings panel, whenever the computer checkbox's check changes.
        private void ComputerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!playingComputer)
            {
                playingComputer = true;
                GameSettingsPanel.Show();

                ColourButton_CheckedChanged(sender, e);
            }
            else
            {
                playingComputer = false;
                GameSettingsPanel.Hide();

                playerColour = Piece.White;
            }
        }

        // This subroutine is called whenever the start game button is clicked, and will create and 
        // display a new chess form, using the selected game settings.
        private void StartGameButton_Click(object sender, EventArgs e)
        {
            // Hide this form.
            Hide();

            // If the player is not facing the computer, set the selected difficulty to five
            // so that the player can recieve the best possible hint moves.
            if (!playingComputer)
            {
                selectedDifficulty = 5;
            }

            // Create a new instance of the chess form, passing in the default fen string, and the other
            // selected game settings. Show this form.
            chess = new ChessForm(Board.DEFAULT_FEN_STRING, playingComputer, selectedDifficulty, playerColour);
            chess.ShowDialog();
        }

        // This subroutine updates the user's player colour whenever the check of one of the colour checkboxes
        // is changed.
        private void ColourButton_CheckedChanged(object sender, EventArgs e)
        {
            if (WhiteColourButton.Checked)
            {
                playerColour = Piece.White;
            }
            else if (BlackColourButton.Checked)
            {
                playerColour = Piece.Black;
            }
            else
            {
                playerColour = randomisedColour;
            }
        }
    }
}

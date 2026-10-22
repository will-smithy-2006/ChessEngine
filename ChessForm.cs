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
using System.Collections;

namespace ChessEngineNS
{
    // The purpose of this class is to provide a graphical user interface for the Chess Board, and to control the flow of the game
    // based on user driven events.
    public partial class ChessForm : Form
    {
        // A resource manager is needed so that the image files for pieces can be fetched from a file.
        private ResourceManager rm;
        // This is where the main instance of the board class is defined.
        private Board board;

        private int screenXOffset;
        private int screenYOffset;

        // These variables all allow the board to highlight all the possible moves, for the currently selected piece.
        // The list of moves represents all the current available moves, whereas the list of selected squares, represents all the integer values for squares that
        // should be highlighted. The isPieceSelected boolean, indicates whether or not a piece is currently selected, and thus whether any squares should be highlighted.
        private List<Move> currentMoves;

        private int[] highlightedSquares;
        private bool isPieceSelected;

        private int[] moveHintSquares;

        private int playerColour;
        private int difficulty;

        Dictionary<int, Image> pieceImages;

        Color darkColour;
        Color lightColour;
        Color highlightColour;
        Color hintColour;

        // Within the constructor method, all the member variables are initialised and the form is generated and drawn.
        public ChessForm(string fenString, bool playingComputer, int difficulty, int playerColour)
        {
            InitializeComponent();

            rm = Resources.ResourceManager;
            // When initialising the board class two arguments must be passed. One defines whether the player is playing against 
// a computer or against another player. The other defines the current board layout, and other parameters of the game.
            board = new Board(fenString, playingComputer);
            highlightedSquares = new int[1];
            moveHintSquares = new int[2] { -1, -1 };
            isPieceSelected = false;

            // Set the difficulty and player colour variables to the values provided as arguments.
            this.difficulty = difficulty;
            this.playerColour = playerColour;

            // Calculate the offset for the board.
            ChessForm_Resize(this, new EventArgs());

            // If the player colour is black, then the player must be facing the computer, and thus the computer needs to make
            // the first move.
            if (playerColour == Piece.Black && playingComputer && board.ColourToPlay() == Piece.White)
            {
                bool successful = board.MakeComputerMove(difficulty);

                // If the move failed, then show an error box.
                if (!successful)
                {
                    MessageBox.Show("Computer Move Failed.");
                }
            }

            // Initialise the piece-image dictionary, and iterate over every piece, and add the corresponding image for both 
            // colours to the dictionary, using it's unique piece integer value as the key.
            pieceImages = new Dictionary<int, Image>();
            for (int piece = Piece.Pawn; piece <= Piece.King; piece++)
            {
                pieceImages[Piece.White | piece] = (Image)rm.GetObject(Piece.GetPieceImageID(Piece.White | piece));
                pieceImages[Piece.Black | piece] = (Image)rm.GetObject(Piece.GetPieceImageID(Piece.Black | piece));
            }

            // A light and dark colour are defined for the two different square types, along with a colour for any squares to
            // be highlighted as a possible move.
            darkColour = Color.DarkOliveGreen;
            lightColour = Color.AntiqueWhite;
            highlightColour = Color.FromArgb(150, 255, 255, 0);
            hintColour = Color.FromArgb(150, 255, 0, 0);

            Invalidate();
        }

        // When the form first loads, the invalidate method is called, to force it to render.
        private void ChessForm_Load(object sender, EventArgs e)
        {
            Invalidate();
        }

        // This method is called whenever the form detects when mouse button has been pressed. The purpose of this method
        // is to detect these mouse clicks, and interpret what the computer should do based on where the mouse click occurs
        // and what particular aspect of the form was clicked.
        private void ChessForm_MouseDown(object sender, MouseEventArgs e)
        {
            // If the mouse click occurs outside the bounds of the board, then ignore it, as no pieces can have been clicked.
            if (e.X < screenXOffset || e.X > screenXOffset + 640 || e.Y < screenYOffset || e.Y > screenYOffset + 640)
                return;

            // The mouse X and Y coordinates are converted into a board file and rank (accordingly), which are then converted
            // into a square number. When calculating the file and rank, the X and Y offset must be subtracted from the mouse 
            // coordinates so that they are within the range of 0 <= x,y <= 640 (the board size).
            int file = (e.X - screenXOffset) / 80;
            int rank = (playerColour == Piece.White) ? 7 - ((e.Y - screenYOffset) / 80) : (e.Y - screenYOffset) / 80;
            int square = Board.FileRankToSquare(file, rank);

            // If a piece has not been selected then one of the following events can occur.
            if (!isPieceSelected)
            {
                // If the square clicked is empty, exit the function.
                if (board[square] == Piece.Empty)
                    return;

                // If the square clicked contains a piece, that is not the colour to play, exit the function.
                if (board.ColourToPlay() != (board[square] & Piece.Colour))
                    return;

// Otherwise, call a function to select the piece, and generate all moves for said piece, and then re-draw the form.
                GeneratePieceMoves(square);
                Invalidate();

                return; // Exit function.
            }

            // If a piece has already been selected, and the square clicked contains a piece, that IS the colour to play then
            // change the piece selected, and redraw the board.
            if (board.ColourToPlay() == (board[square] & Piece.Colour))
            {
                GeneratePieceMoves(square);
                Invalidate();

                return;
            }

            // If a piece is selected, and an empty square or opponent piece is clicked, and the square clicked is not contained 
            // within the list of possible moves, de-select this piece and redraw the form.
            if (!highlightedSquares.Contains(square))
            {
                isPieceSelected = false;
                Invalidate();
                return;
            }


            // Otherwise, the square clicked is a valid move for the selected piece. Iterate through all the current moves, and 
            // determine which one has been chosen, and make the move.
            foreach (Move m in currentMoves)
            {
                // If the ending square of the current move, is not the square clicked, continue to the next iteration.
                if (m.EndingSquare != square)
                    continue;

                // Make the move.
                bool successfulMove = board.MakeMove(m);

                playerColour = board.IsPlayingComputer ? playerColour : board.ColourToPlay();
                moveHintSquares = new int[2] { -1, -1 };

                // If the move was successful de-select this piece and redraw the form.
                if (successfulMove)
                {
                    isPieceSelected = false;
                    Refresh();

                    // Look for Checkmate or draw.
                    if (board.Checkmate)
                    {
                        string winningColour = board.ColourToPlay() == Piece.White ? "Black" : "White";
                        MessageBox.Show($"Checkmate - {winningColour} Wins!");
                    }
                    else if (board.Stalemate)
                    {
                        MessageBox.Show("Stalemate - Draw!");
                    }

                    // If the player is competing against the computer, call the board to make a computer move, and if the move 
                    // is successfully made, redraw the form.
                    if (board.IsPlayingComputer)
                    {
                        bool successfulComputerMove = board.MakeComputerMove(difficulty);

                        if (!successfulComputerMove && !board.Checkmate)
                            MessageBox.Show("Computer Move Failed");

                        Refresh();
                    }
                }

                // Look for Checkmate or draw.
                if (board.Checkmate)
                {
                    string winningColour = board.ColourToPlay() == Piece.White ? "Black" : "White";
                    MessageBox.Show($"Checkmate - {winningColour} Wins!");
                }
                else if (board.Stalemate)
                {
                    MessageBox.Show("Stalemate - Draw!");
                }
            }

        }

        // The purpose of this method is to "select" the piece at a given square. To select a piece the method first
        // sets the isPieceSelected boolean to true, and then estabishes the list of possible moves, and sets the highlighted 
        // squares to all end squares of these moves.
        private void GeneratePieceMoves(int square)
        {
            isPieceSelected = true;

            currentMoves = board.GenerateValidMoves(square);
            highlightedSquares = new int[currentMoves.Count];

            for (int i = 0; i < currentMoves.Count; i++)
            {
                highlightedSquares[i] = currentMoves[i].EndingSquare;
            }
        }

        // This subroutine will calculate the X,Y offset on the screen from which the chess board should be drawn from.
        private void ChessForm_Resize(object sender, EventArgs e)
        {
            screenXOffset = (this.ClientSize.Width / 2) - 320;
            screenYOffset = (this.ClientSize.Height / 2) - 320;
        }

        // The purpose of this method is to draw the chess board, and all the pieces onto the form.
        private void ChessForm_Paint(object sender, PaintEventArgs e)
        {
            // A brush object and a reference to the form graphics are created.
            SolidBrush brush = new SolidBrush(Color.Black);
            Graphics graphics = e.Graphics;

            // An 8x8 board is drawn with alternating square colours.
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    brush.Color = ((x + y) % 2 == 0) ? lightColour : darkColour;

                    if (playerColour == Piece.Black)
                    {
                        brush.Color = (brush.Color == darkColour) ? lightColour : darkColour;
                    }

                    graphics.FillRectangle(brush, new Rectangle((x * 79) + x + screenXOffset, (y * 80) + screenYOffset, 80, 80));
                }
            }

            // If a piece is selected then the highlighted squares are iterated through to change their colour.
            if (isPieceSelected)
            {
                foreach (int square in highlightedSquares)
                {
                    // Calculate the screen X,Y coordinates of the current square, ensuring to flip the orientation of the board
                    // if the current player colour is black.
                    int screenX = (Board.File(square) * 79) + Board.File(square);
                    int screenY = (playerColour == Piece.White) ? (7 - Board.Rank(square)) * 80 : Board.Rank(square) * 80;

                    brush.Color = highlightColour;
                    // Repaint the corresponding square on the screen, adding the X,Y offset to the calculate screen 
                    // coordinates, to get the correct screen position.s
                    graphics.FillRectangle(brush, new Rectangle(screenX + screenXOffset, screenY + screenYOffset, 80, 80));
                }
            }

            // If there is a hint currently to be displayed on the board, highlight the corresponding squares.
            if (moveHintSquares[0] != -1)
            {
                foreach (int square in moveHintSquares)
                {
                    int screenX = (Board.File(square) * 79) + Board.File(square);
                    int screenY = (playerColour == Piece.White) ? (7 - Board.Rank(square)) * 80 : Board.Rank(square) * 80;

                    brush.Color = hintColour;
                    graphics.FillRectangle(brush, new Rectangle(screenX + screenXOffset, screenY + screenYOffset, 80, 80));
                }
            }

// The 64-Integer board array is iterated through (skipping over empty squares), and any pieces are drawn to the screen.
            for (int i = 0; i < 64; i++)
            {
                // Skip over empty squares.
                if (board[i] == Piece.Empty)
                    continue;

                // Retrieve the Image File for the piece at this given location on the board.
                Image img = pieceImages[board[i]];

                // Calculate the screen X and Y positions.
                int screenX = Board.File(i) * 80;
                int screenY = (playerColour == Piece.White) ? (7 - Board.Rank(i)) * 80 : Board.Rank(i) * 80;

                // Draw the piece to the screen in it's corresponding position (applying the offset).
                graphics.DrawImage(img, screenX + screenXOffset, screenY + screenYOffset);
            }
        }

        // This subroutine converts the current game state into a series of strings which are then written
        // to a text file, which can be read from at a later point, to load the current game.
        // This subroutine is called whenever the save game button is clicked.
        private void SaveGameButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Open the game save text file using a stream writer object.
                using (StreamWriter sw = new StreamWriter(MainMenu.saveFilePath))
                {
                    // Generate the fen string for the current board and write it to the text file.
                    string fenString = board.GenerateFENStringFromBoard(board.GetBoard());
                    sw.WriteLine(fenString);

                    // If the player is facing the computer, write a boolean string to indicate so, and 
                    // the difficulty integer value to the text file.
                    if (board.IsPlayingComputer)
                    {
                        sw.WriteLine("true");
                        sw.WriteLine(difficulty);
                    }
                    else
                    {
                        sw.WriteLine("false");
                    }

                    // Write the player's colour to the text file.
                    sw.WriteLine(playerColour);
                }
            }
            // If there are any errors while performing then display the corresponding error message.
            catch (Exception ex)
            {
                Trace.WriteLine($"The game could not be saved. Error Message: {ex.Message}");
            }
        }

        // This subroutine closes the form application.
        private void ExitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // This subroutine generate's the best possible move for the current colour to play
        // in the current position, adds the start and end squares of this to the move hint squares
        // array, and calls for the form to be redrawn. This method is triggered whenever the user
        // clicks the hint button.
        private void HintButton_Click(object sender, EventArgs e)
        {
            Move move = board.GenerateComputerMove(difficulty);

            moveHintSquares[0] = move.StartingSquare;
            moveHintSquares[1] = move.EndingSquare;

            Invalidate();
        }
    }
}

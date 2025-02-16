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

        // These variables all allow the board to highlight all the possible moves, for the currently selected piece.
        // The list of moves represents all the current available moves, whereas the list of selected squares, represents all the integer values for squares that
        // should be highlighted. The isPieceSelected boolean, indicates whether or not a piece is currently selected, and thus whether any squares should be highlighted.
        private List<Move> currentMoves;
        private int[] highlightedSquares;
        private bool isPieceSelected;

        // Within the constructor method, all the member variables are initialised and the form is generated and drawn.
        public ChessForm()
        {
            InitializeComponent();

            rm = Resources.ResourceManager;
            // When initialising the board class two arguments must be passed. One defines whether the player is playing against a computer
            // or against another player. The other defines the current board layout, and other parameters of the game.
            board = new Board(playingComputer: true, fenString: "n1P4b/8/1P3p2/8/8/8/8/k6K w");
            highlightedSquares = new int[1];
            isPieceSelected = false;

            Invalidate();
        }

        // When the form first loads, the invalidate method is called, to force it to render.
        private void ChessForm_Load(object sender, EventArgs e)
        {
            Invalidate();
        }

        // This method is called whenever the form detects when mouse button has been pressed. The purpose of this method
        // is to detect these mouse clicks, and intepret what the computer should do based on where the mouse click occurs
        // and what particular aspect of the form was clicked.
        private void ChessForm_MouseDown(object sender, MouseEventArgs e)
        {
            // The mouse X and Y coordinates are converted into a board file and rank (accordingly), which are then converted
            // into a square number.
            int file = e.X / 80;
            int rank = 7 - (e.Y / 80);
            int square = Board.FileRankToSquare(file, rank);
            
            // If a piece has not been selected then one of the following events can occur.
            if(!isPieceSelected)
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

            // If a piece is selected, and an empty square or opponent piece is clicked, and the square clicked is not contained within the list of possible moves,
            // de-select this piece and redraw the form.
            if (!highlightedSquares.Contains(square))
            {
                isPieceSelected = false;
                Invalidate();
                return;
            }

            
            // Otherwise, the square clicked is a valid move for the selected piece. Iterate through all the current moves, and determine which
            // one has been chosen, and make the move.
            foreach (Move m in currentMoves) 
            {
                // If the ending square of the current move, is not the square clicked, continue to the next iteration.
                if (m.EndingSquare != square)
                    continue;
                
                // Make the move.
                bool successfulMove = board.MakeMove(m);
                
                // If the move was successful de-select this piece and redraw the form.
                if(successfulMove)
                {
                    isPieceSelected = false;
                    Refresh();

                    // If the player is competing against the computer, call the board to make a computer move, and if the move is
                    // successfully made, redraw the form.
                    if (board.IsPlayingComputer)
                    {
                        bool successfulComputerMove = board.MakeComputerMove();

                        if (!successfulComputerMove)
                            MessageBox.Show("Computer Move Failed");

                        Refresh();
                    }

                    return;
                }
            }

        }

        // The purpose of this method is to "select" the piece at a given square. To select a piece the method first
        // sets the isPieceSelected boolean to true, and then estabishes the list of possible moves, and sets the highlighted squares
        // to all end squares of these moves.
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

        // The purpose of this method is to draw the chess board, and all the pieces onto the form.
        private void ChessForm_Paint(object sender, PaintEventArgs e)
        {
            // A brush object and a reference to the form graphics are created.
            SolidBrush brush = new SolidBrush(Color.Black);
            Graphics graphics = e.Graphics;

            // A light and dark colour are defined for the two different square types, along with a colour for any squares to
            // be highlighted as a possible move.
            Color darkColour = Color.DarkOliveGreen;
            Color lightColour = Color.AntiqueWhite;
            Color highlightColour = Color.FromArgb(150, 255, 255, 0);

            // An 8x8 board is drawn with alternating square colours.
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    brush.Color = ((x + y) % 2 == 0) ? darkColour : lightColour;
                    graphics.FillRectangle(brush, new Rectangle((x * 79) + x, y * 80, 80, 80));
                }
            }

            // If a piece is selected then the highlighted squares are iterated through to change their colour.
            if (isPieceSelected)
            {
                foreach (int square in highlightedSquares)
                {
                    int screenX = (Board.File(square) * 79) + Board.File(square);
                    int screenY = (7 - Board.Rank(square)) * 80;

                    brush.Color = highlightColour;
                    graphics.FillRectangle(brush, new Rectangle(screenX, screenY, 80, 80));
                }
            }

            // The 64-Integer board array is iterated through (skipping over empty squares), and any pieces are drawn to the screen.
            for (int i = 0; i < 64; i++)
            {
                if (board[i] == Piece.Empty)
                    continue;

                // Retrieve the Image File for the piece at this given location on the board.
                Image img = (Image)rm.GetObject(Piece.GetPieceImageID(board[i]));

                // Calculate the screen X and Y positions.
                int screenX = Board.File(i) * 80;
                int screenY = (7 - Board.Rank(i)) * 80;

                // Draw the piece to the screen in it's corresponding position.
                graphics.DrawImage(img, screenX, screenY);
            }
        }
    }
}
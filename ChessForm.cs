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

namespace ChessEngine
{
    public partial class ChessForm : Form
    {
        private ResourceManager rm = Resources.ResourceManager;


        private Board board;

        private int[] selectedSquares;
        private List<Move> currentMoves;
        private bool isPieceSelected;


        public ChessForm()
        {
            InitializeComponent();

            board = new Board("4k2r/6r1/8/8/8/8/3R4/R3K3 w");
            selectedSquares = new int[1];
            isPieceSelected = false;

            Invalidate();
        }

        private void ChessForm_Load(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void ChessForm_MouseDown(object sender, MouseEventArgs e)
        {
            int file = e.X / 80;
            int rank = board.whiteToPlay ? 7 - (e.Y / 80) : e.Y / 80;
            int square = Board.FileRankToSquare(file, rank);
            
            if(!isPieceSelected)
            {
                if (board[square] == Piece.Empty)
                    return;

                if (board.ColourToPlay() != (board[square] & Piece.Colour))
                    return;

                GeneratePieceMoves(square);
                Invalidate();
                
                return;

            }

            if (board.ColourToPlay() == (board[square] & Piece.Colour))
            {
                GeneratePieceMoves(square);
                Invalidate();
                return;
            }

            if (!selectedSquares.Contains(square))
            {
                isPieceSelected = false;
                Invalidate();
                return;
            }

            foreach (Move m in currentMoves) 
            {
                if (m.EndingSquare != square)
                    continue;
                
                bool successfulMove = board.MakeMove(m);
                    
                if(successfulMove)
                {
                    isPieceSelected = false;
                    Invalidate();

                    return;
                }
            }

        }

        private void GeneratePieceMoves(int square)
        {
            isPieceSelected = true;

            currentMoves = board.GenerateValidMoves(square);
            selectedSquares = new int[currentMoves.Count];

            for (int i = 0; i < currentMoves.Count; i++)
            {
                selectedSquares[i] = currentMoves[i].EndingSquare;
            }
        }

        private void ChessForm_Paint(object sender, PaintEventArgs e)
        {
            SolidBrush brush = new SolidBrush(Color.Black);
            Graphics graphics = e.Graphics;

            Color darkColour = Color.DarkOliveGreen;
            Color lightColour = Color.AntiqueWhite;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    brush.Color = ((x + y) % 2 == 0) ? darkColour : lightColour;
                    graphics.FillRectangle(brush, new Rectangle((x * 79) + x, y * 80, 80, 80));
                }
            }

            if (isPieceSelected)
            {
                foreach (int square in selectedSquares)
                {
                    int screenX = (Board.File(square) * 79) + Board.File(square);
                    int screenY = board.whiteToPlay ? (7 - Board.Rank(square)) * 80 : Board.Rank(square) * 80;

                    brush.Color = Color.Yellow;
                    graphics.FillRectangle(brush, new Rectangle(screenX, screenY, 80, 80));
                }
            }


            for (int i = 0; i < 64; i++)
            {
                if (board[i] == Piece.Empty)
                    continue;

                Image img = (Image)rm.GetObject(Piece.GetPieceImageID(board[i]));

                int screenX = Board.File(i) * 80;
                int screenY = board.whiteToPlay ? (7 - Board.Rank(i)) * 80 : Board.Rank(i) * 80;

                graphics.DrawImage(img, screenX, screenY);
            }
        }
    }
}
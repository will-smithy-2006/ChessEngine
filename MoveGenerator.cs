using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    // An instance of this class will be used to generate moves for both players, each will use their own instance,
    // in order to keep track of the friendly colours and whomever's turn it is to play.
    internal class MoveGenerator
    {    
        enum OrthogonalMoves
        {
            Forward = 8,
            Backward = -8,
            Left = -1,
            Right = 1
        }

        enum DiagonalMoves
        {
            ForwardLeft = 7,
            ForwardRight = 9,
            BackwardLeft = -9,
            BackwardRight = -7,
        }

        public int[] KnightMoves = { -15, 15, -17, 17, -10, 10, -6, 6 };
        public int[] KingMoves = { -1, 1, -7, 7, -8, 8, -9, 9 };      

        private int friendlyColour;
        private int opponentColour;
        private int friendlyListIndex;
        private int opponentListIndex;

        private bool whiteToPlay;
        private bool inCheck;

        private Board board;

        public MoveGenerator()
        {
            friendlyColour = Piece.White;
            opponentColour = Piece.Black;
            friendlyListIndex = friendlyColour == Piece.White ? 0 : 1;
            opponentListIndex = opponentColour == Piece.White ? 0 : 1;

            whiteToPlay = true;
            inCheck = false;
        }

        public MoveGenerator(Board board)
        {
            this.board = board;
            this.whiteToPlay = board.whiteToPlay;

            this.friendlyColour = board.FriendlyColour;
            this.opponentColour = board.OpponentColour;

            friendlyListIndex = friendlyColour == Piece.White ? 0 : 1;
            opponentListIndex = opponentColour == Piece.White ? 0 : 1;
        }

        private void Initialise(Board board)
        {
            this.board = board;
            this.whiteToPlay = board.whiteToPlay;

            this.friendlyColour = whiteToPlay ? Piece.White : Piece.Black;
            this.opponentColour = whiteToPlay ? Piece.Black : Piece.White;

            friendlyListIndex = friendlyColour == Piece.White ? 0 : 1;
            opponentListIndex = opponentColour == Piece.White ? 0 : 1;
        }

        public List<Move> GenerateLegalMoves(int square, Board board)
        {
            Initialise(board);

            int[] squares = this.board.GetBoard();

            List<Move> legalMoves = new List<Move>();

            int piece = board[square];

            if (piece == Piece.Empty)
                return legalMoves;

            if (Piece.IsKing(piece) || Piece.IsKnight(piece))
                legalMoves = GenerateStaticMoves(square, squares);

            if (Piece.IsSlidingPiece(piece))
                legalMoves = GenerateSlideMoves(square, squares);

            if (Piece.IsPawn(piece))
                legalMoves = GeneratePawnMoves(square, squares);

            return legalMoves;
        }

   
        // This function generates all moves for a given sliding piece e.g. A Rook, Bishop, or Queen
        public List<Move> GenerateSlideMoves(int square, int[] board)
        {
            List<Move> moves = new List<Move>();
            int pieceType = board[square] & Piece.Type;

            if (pieceType == Piece.Rook || pieceType == Piece.Queen)
            {
                foreach (int direction in Enum.GetValues(typeof(OrthogonalMoves)))
                {
                    int currentSquare = square + direction;

                    while(Board.InBoard(currentSquare))
                    {
                        if (!(Board.IsSameRank(square, currentSquare) || Board.IsSameFile(square, currentSquare)))
                            break;

                        if ((board[currentSquare] & Piece.Colour) == friendlyColour)
                            break;

                        moves.Add(new Move(square, currentSquare));

                        if ((board[currentSquare] & Piece.Colour) == opponentColour)
                            break;

                        currentSquare += direction;
                    }
                }
            }

            if (pieceType == Piece.Bishop || pieceType == Piece.Queen)
            {
                foreach (int direction in Enum.GetValues(typeof(DiagonalMoves)))
                {
                    int currentSquare = square + direction;

                    while (Board.InBoard(currentSquare))
                    {
                        if (!Board.AreAdjacentRanks(currentSquare - direction, currentSquare))
                            break;

                        if ((board[currentSquare] & Piece.Colour) == friendlyColour)
                            break;

                        moves.Add(new Move(square, currentSquare));

                        if ((board[currentSquare] & Piece.Colour) == opponentColour)
                            break;

                        currentSquare += direction;
                    }
                }
            }

            return moves;
        }

        // This Function generates the moves for the King and Knight pieces, as they both have static movement patterns,
        // unlike sliding pieces, or pawns which have more complicated moves.

        // NOTE: This may need to be edited for castling to be introduced.
        public List<Move> GenerateStaticMoves(int square, int[] board)
        {
            List<Move> moves = new List<Move>();

            int[] staticMoves;

            switch (board[square] & Piece.Type)
            {
                case Piece.Knight:
                    staticMoves = KnightMoves;
                    break;
                case Piece.King:
                    staticMoves = KingMoves;
                    break;
                default:
                    return moves;
            }
            
            foreach (int increment in staticMoves)
            {
                int endSquare = square + increment;

                // If move is outside board, disregard it.
                if (!Board.InBoard(endSquare))
                    continue;

                // If neither the file or rank of the target square are adjacent to that of the starting square, then there
                // is a wrap-around error and the move is invalid.
                if (!Board.AreAdjacentRanks(square, endSquare) && !Board.AreAdjacentFiles(square, endSquare))
                    continue;

                // If the move is either empty, or an opponent piece then it is a valid move and can be added to the list.
                if ((board[endSquare] & Piece.Colour) == opponentColour || board[endSquare] == Piece.Empty)
                {
                    moves.Add(new Move(square, endSquare));
                }
            }

            return moves;
        }

        public List<Move> GeneratePawnMoves(int square, int[] board)
        {
            List<Move> moves = new List<Move>();
           
            int piece = board[square];
            int direction = Piece.IsWhite(piece) ? 1 : -1;

            int frontMove = square + (direction * (int)OrthogonalMoves.Forward);
            int doubleFrontMove = frontMove + (direction * (int)OrthogonalMoves.Forward);


            // First check to see if the front move is in the board, and then check to see if it is empty.
            if (Board.InBoard(frontMove) && board[frontMove] == Piece.Empty)
            {
                // If empty then the move is valid (pawn cannot take forwards).
                moves.Add(new Move(square, frontMove));

                // The front move must be valid in order for the double front move to be valid.
                // Check to see if the pawn has moved yet (pawns cannot move backwards), then check to see if the double move square is empty, if so the move is valid.
                if(Board.Rank(square) == 1 && Piece.IsWhite(board[square]) && board[doubleFrontMove] == Piece.Empty)
                {
                    moves.Add(new Move(square, doubleFrontMove));
                }
                else if(Board.Rank(square) == 6 && Piece.IsBlack(board[square]) && board[doubleFrontMove] == Piece.Empty)
                {
                    moves.Add(new Move(square, doubleFrontMove));
                }
            }
            
            // Captures:3
            int leftCapture = square + (direction * (int)DiagonalMoves.ForwardLeft);
            int rightCapture = square + (direction * (int)DiagonalMoves.ForwardRight);

            // The capture moves are valid if the move is within the board bounds, and if there is an opponent
            // piece residing on the target square.
            if (Board.InBoard(leftCapture) && (board[leftCapture] & Piece.Colour) == opponentColour && Board.AreAdjacentFiles(square, leftCapture))
            {
                moves.Add(new Move(square, leftCapture));
            }

            if (Board.InBoard(rightCapture) && (board[rightCapture] & Piece.Colour) == opponentColour && Board.AreAdjacentFiles(square, rightCapture))
            {
                moves.Add(new Move(square, rightCapture));
            }

            return moves;
        }
    }
}

/*
 7 x x x x x x x x
 6 x x x x x x x x
 5 x x x x x x x x 
 4 x x x x x x x x
 3 x x x N x x x x
 2 x x x x x x x x
 1 x x x x x k x x
 0 x x x x x x x x
   0 1 2 3 4 5 6 7
 */
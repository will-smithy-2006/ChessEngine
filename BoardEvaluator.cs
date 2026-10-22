using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    // The purpose of this class is to perform a static evaluation of a given board.
    public class BoardEvaluator
    {
        // These constants are integer values used to dictate the "worth" of each piece type.
        // The king's piece value is the maximum integer value since it is invaluable.
        // These values are absolute and do not take into account the colour of a piece.
        public const int PawnValue = 100;
        public const int KnightValue = 300;
        public const int BishopValue = 350;
        public const int RookValue = 500;
        public const int QueenValue = 900;

        // This dictionary uses a piece-type integer value as a key and stores the corresponding Piece-Value
        // as the key's value.
        public static readonly Dictionary<int, int> PieceValues = new Dictionary<int, int>()
        {
            { Piece.Empty, 0 },
            { Piece.Pawn, PawnValue },
            { Piece.Knight, KnightValue },
            { Piece.Bishop, BishopValue },
            { Piece.Rook, RookValue },
            { Piece.Queen, QueenValue },
            { Piece.King, 0 }
        };

        // The following tables are called Piece-Square Tables and assign a score to every square on the chess board for 
        // each piece type. These score can positively or negatively affect a piece's evaluation. It should be noted that
        // the scores on all the squares do not take into account the colour of a piece, and so must be multiplied by -1 when
        // being used by black. Within these tables, Table[0] = A1, Table[1] = A2, ... Table[63] = H8.

        // This is the piece-square table for a pawn. This table is directional, and so the ranks should be flipped
        // when used for a black pawn (meaning Table[63] = H1).
        public static readonly int[] PawnTable = new int[64]
        {
            0,   0,   0,   0,   0,   0,  0,  0,
            5,   5,   5, -20, -20,   5,  5,  5,
            5,   0,   0,   5,   5,   0,  0,  5,
            5,   5,   5,  25,  25,   5,  5,  5,
            10, 10,  15,  25,  25,  15, 10, 10,
            20, 20,  20,  30,  30,  20, 20, 20,
            35, 35,  35,  45,  45,  35, 35, 35,
            0,   0,   0,   0,   0,   0,  0,  0,

        };

        // This is the piece-square table for a knight.
        public static readonly int[] KnightTable = new int[64]
        {
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -20,   0,   5,   5,   0, -20, -40,
            -30,   0,  10,  15,  15,  10,   0, -30,
            -30,   5,  15,  20,  20,  15,   5, -30,
            -30,   5,  15,  20,  20,  15,   5, -30,
            -30,   0,  10,  15,  15,  10,   0, -30,
            -40, -20,   0,   5,   5,   0, -20, -40,
            -50, -40, -30, -30, -30, -30, -40, -50,
        };

        // This is the piece-square table for a bishop.
        public static readonly int[] BishopTable = new int[64]
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
            -10,   0,   5,   5,   5,   5,   0, -10,
            -10,   5,  10,  10,  10,  10,   5, -10,
            -10,   5,  10,  15,  15,  10,   5, -10,
            -10,   5,  10,  15,  15,  10,   5, -10,
            -10,   5,  10,  10,  10,  10,   5, -10,
            -10,   0,   5,   5,   5,   5,   0, -10,
            -20, -10, -10, -10, -10, -10, -10, -20
        };

        // This is the piece-square table for a rook.
        public static readonly int[] RookTable = new int[64]
        {
            0,  0,  0,  5,  5,  0,  0, 0,
            0,  0,  5, 10, 10,  5,  0, 0,
            0,  5, 10, 15, 15, 10,  5, 0,
            5, 10, 15, 20, 20, 15, 10, 5,
            5, 10, 15, 20, 20, 15, 10, 5,
            0,  5, 10, 15, 15, 10,  5, 0,
            0,  0,  5, 10, 10,  5,  0, 0,
            0,  0,  0,  5,  5,  0,  0, 0
        };

        // This is the piece-square table for a queen.
        public static readonly int[] QueenTable = new int[64]
        {
            -20, -10, -10, -5, -5, -10, -10, -20,
            -10,  0,    5, 10, 10,   5,   0, -10,
            -10,  5,   10, 15, 15,  10,   5, -10,
            -5,  10,   15, 20, 20,  15,  10,  -5,
            -5,  10,   15, 20, 20,  15,  10,  -5,
            -10,  5,   10, 15, 15,  10,  5,  -10,
            -10,  0,    5, 10, 10,   5,  0,  -10,
            -20, -10, -10, -5, -5, -10, -10, -20
        };

        // This is the piece square table for a king. This table is directional, and so the ranks should be flipped
        // when used for a black king (meaning Table[63] = H1).
        public static readonly int[] KingTable = new int[64]
        {
             20,  30,  10,   0,   0,  10,  30,  20,
             20,  20,   0,   0,   0,   0,  20,  20,
            -10, -20, -20, -20, -20, -20, -20, -10,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
        };



        // This dictionary uses a piece-type integer value (as defined in piece class) as a key and as the value, stores the 
        // corresponding Piece-Square Table for the given piece. This is used for quick-access to the tables removing the need 
        // for long if-else chains, determining piece type.
        public static readonly Dictionary<int, int[]> PieceSquareTables = new Dictionary<int, int[]>()
        {
            {Piece.Empty, new int[64] },
            {Piece.Pawn, PawnTable },
            {Piece.Knight, KnightTable },
            {Piece.Bishop, BishopTable },
            {Piece.Rook, RookTable },
            {Piece.Queen, QueenTable },
            {Piece.King, KingTable }
        };
        
        // This function takes a board object, and performs an evaluation of the current position, returning an integer score
        // with negative (smaller) scores favouring black, and positive (bigger) scores favouring white.
        public static int Evaluate(Board board)
        {
            int evaluation = 0;
            
            // If the current position is checkmate, the function will return the worst possible score +/- 1
            // (i.e. maximum or minimum integer value) for the colour to play (the colour in checkmate).
            if (board.Checkmate)
            {
                if (board.ColourToPlay() == Piece.White)
                    return int.MinValue + 1;
                else
                    return int.MaxValue - 1;
            }
            else if (board.Stalemate)
            {
                // If the board is in a state of stalemate, then zero should be returned since the game is drawn.
                return 0;
            }

            // Perform a static evaluation of all the material on the board.
            evaluation += EvaluateMaterial(board.GetBoard());
            
            return evaluation;
        }


        // This function takes a board array and returns an evaluation score based on the material residing on the board.
        private static int EvaluateMaterial(int[] board)
        {
            int material = 0;

            // The loop iterates through all the squares on the board (skipping empty squares) and
            // calculating the sum of all piece values on the board (with the piece-value negated when it is black).
            for (int i = 0; i < board.Length; i++)
            {
                // Skip empty square.
                if (board[i] == Piece.Empty)
                    continue;

                material += GetPieceValue(board[i], i);
            }

            return material;
        }

        // This function takes a piece and the square it resides upon and returns it's material score (taking into account it's 
        // colour). The score returned will be negative if it favours black, and positive if it favours white.
        public static int GetPieceValue(int piece, int square)
        {
            // Calculate a multiplier for the piece's material score (-1 if black, and +1 if white).
            int colour = Piece.IsWhite(piece) ? 1 : -1;

            // If the piece provided as an argument is a pawn or king, the access index for the piece-square table must be
            // calculated since the table's for these pieces are directional based on the colour.
            if (Piece.IsPawn(piece) || Piece.IsKing(piece))
            {
                // If the piece is white, access the table like normal, otherwise access the table from the other side using the 
                // same file, but the rank on the opposite end of the chess board, since black pieces are located at the top of 
                // the board, but the piece-square table starts from the bottom.
                square = Piece.IsWhite(piece) ? square : Board.FileRankToSquare(Board.File(square), 7 - Board.Rank(square));
            }

            // Get the base value of the piece provided as an argument, add the corresponding position-score for this piece
// on the square provided as an argument. Multiply the result by the colour-multiplied (-1 if black, +1 if white) and return the result.
            return colour * (PieceValues[piece & Piece.Type] + PieceSquareTables[piece & Piece.Type][square]);
        }
    }
}

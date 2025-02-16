using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    // The purpose of this class is to perform a static evaluation of a given board.
    public static class BoardEvaluator
    {
        // Piece Values used for static evaluation.
        public const int PawnValue = 100;
        public const int KnightValue = 300;
        public const int BishopValue = 350;
        public const int RookValue = 500;
        public const int QueenValue = 900;

        
        // This function takes a board object, and performs an evaluation of the current position, returning an integer score
        // with negative (smaller) scores favouring black, and positive (bigger) scores favouring white.
        public static int Evaluate(Board board)
        {
            int evaluation = 0;
            
            // If the current position is checkmate, the function will return the worst possible score 
            // (i.e. maximum or minimum integer value) for the colour to play (the colour in checkmate).
            if (board.Checkmate)
            {
                if (board.ColourToPlay() == Piece.White)
                    return int.MinValue;
                else
                    return int.MaxValue;
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

                // Establish a piece-value multiplier of -1 or 1 (black or white).
                int colour = Piece.IsWhite(board[i]) ? 1 : -1;

                // Add piece-value (applying the multiplier) for corresponding piece-type.
                switch(board[i] & Piece.Type)
                {
                    case Piece.Pawn:
                        material += colour * PawnValue;
                        break;
                    case Piece.Knight:
                        material += colour * KnightValue;
                        break;
                    case Piece.Bishop:
                        material += colour * BishopValue;
                        break;
                    case Piece.Rook:
                        material += colour * RookValue;
                        break;
                    case Piece.Queen:
                        material += colour * QueenValue;
                        break;
                }

            }
            return material;
        }
    }
}

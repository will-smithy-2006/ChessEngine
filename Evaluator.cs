using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    internal class Evaluator
    {
        // Piece Values
        public const int PawnValue = 100;
        public const int KnightValue = 300;
        public const int BishopValue = 350;
        public const int RookValue = 500;
        public const int QueenValue = 900;

        public static int Evaluate(Board board)
        {
            int evaluation = 0;
            
            if (board.Checkmate)
            {
                if (board.ColourToPlay() == Piece.White)
                    return int.MaxValue;
                else
                    return int.MinValue;
            }

            evaluation += EvaluateMaterial(board.GetBoard());
            
            return evaluation;
        }

        public static int EvaluateMaterial(int[] board)
        {
            int material = 0;
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == Piece.Empty)
                    continue;

                int colour = Piece.IsWhite(board[i]) ? 1 : -1;

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


        // The purpose of this function is to recursively search through all the moves and response moves on the chess board passed, to a given depth
        // and evaluate the best possible move to make for the current colour to play. It does this by creating a tree of moves, branches of which are 'pruned'
        // for efficiency. Pruning occurs when the current best possible evaluation for the black moves is less than the best possible evaluation of the white moves.

        // NOTE: Best moves for black are indicated by (more) negative values, and best moves for white are indicated by (more) positive values.

        // This function returns an integer tuple, the first item is the best possible evaluation, and the second item is the index of the move that causes
        // this evaluation.
        public (int, int) MiniMaxMoveEvaluation(Board board, int depth, int bestEvaluationWhite, int bestEvaluationBlack)
        {
            // When the function reaches the bottom of the tree of possible moves (based on the defined depth)
            // it will return a "static evaluation" of the board, and sets the index of the move to -1.
            if (depth == 0 || board.Checkmate)
            {
                System.Diagnostics.Trace.WriteLine($"L0: {Evaluate(board)}");
                return (Evaluate(board), -1);
            }

            // An instance of a move generator object is created, and is used to generate a list of all the legal moves in the current
            // position.
            MoveGenerator moveGenerator = new MoveGenerator(board);
            List<Move> moveList = moveGenerator.GenerateAllLegalMoves(board);

            if (board.ColourToPlay() == Piece.White)
            {
                // The Max evaluation is set to the smallest possible value, as this is the worst possible evaluation for white.
                int maxEvaluation = int.MinValue;
                int maxEvalIndex = -1;

                System.Diagnostics.Trace.WriteLine($"White To Play:\nMax (Best White) Evaluation: {maxEvaluation}\nBest Evaluation White: {bestEvaluationWhite}\nBest Evaluation Black: {bestEvaluationBlack}");

                // This loop iterates through the list of all legal moves, evaluating each move, and tracking the best possible move.
                for(int i = 0; i < moveList.Count; i++) 
                {
                    // Get current move from move list.
                    Move boardMove = moveList[i];
                    
                    // The current move is made on the board, so that this new position can be evaluated. Next, this function is called 
                    // recursively, passing in the new board position, and decrementing the depth indicator by one. Once this function has returned,
                    // the current move is unmade, returning the board to it's original state.
                    board.MakeMove(boardMove);
                    System.Diagnostics.Trace.WriteLine($"\nEntering L{depth}/B{i}. ({boardMove.MoveName()})");
                    (int, int) evaluation = MiniMaxMoveEvaluation(board, depth - 1, bestEvaluationWhite, bestEvaluationBlack);
                    board.UnmakeMove();

                    // If the current evaluation score is better for white (i.e. greater) than current max evaluation, then the max evaluation and its index are
                    // updated. The best white evaluation variable is then set to the greater value of itself, and the current evaluation score.
                    if (evaluation.Item1 > maxEvaluation) {
                        maxEvaluation = evaluation.Item1;
                        maxEvalIndex = i;
                        System.Diagnostics.Trace.WriteLine($"Max Value ({maxEvaluation}), Index ({maxEvalIndex})");
                    }
                    bestEvaluationWhite = Math.Max(bestEvaluationWhite, evaluation.Item1);

                    System.Diagnostics.Trace.WriteLine($"Returning {evaluation.Item1} ({moveList[maxEvalIndex].MoveName()}) to L{depth}");

                    // If the best evaluation for black is less than (or equal) to the best evaluation for white, then the remaining of the branches
                    // of the move tree (at this depth) can be pruned, and thus are not evaluated.
                    if (bestEvaluationBlack <= bestEvaluationWhite)
                    {
                        System.Diagnostics.Trace.WriteLine($"L{depth}/B{i + 1}-{moveList.Count} Pruned");
                        break;
                    }
                }

                return (maxEvaluation, maxEvalIndex);
            }
            else
            {
                // The minimum evaluation is set to the biggest possible value, as this is the worst evaluation for black.
                int minEvaluation = int.MaxValue;
                int minEvalIndex = -1;

                System.Diagnostics.Trace.WriteLine($"\nBlack To Play:\nMin (Best Black) Evaluation: {minEvaluation}\nBest Evaluation White: {bestEvaluationWhite}\nBest Evaluation Black: {bestEvaluationBlack}");

                // This loop iterates through the list of all legal moves, evaluating each move, and tracking the best possible move.
                for (int i = 0; i < moveList.Count; i++)
                {
                    // Get current move from move list.
                    Move boardMove = moveList[i];

                    // The current move is made on the board, so that this new position can be evaluated. Next, this function is called 
                    // recursively, passing in the new board position, and decrementing the depth indicator by one. Once this function has returned,
                    // the current move is unmade, returning the board to it's original state.
                    board.MakeMove(boardMove);
                    System.Diagnostics.Trace.WriteLine($"\nEntering L{depth}/B{i}. ({boardMove.MoveName()})\n");
                    (int, int) evaluation = MiniMaxMoveEvaluation(board, depth - 1, bestEvaluationWhite, bestEvaluationBlack);
                    board.UnmakeMove();

                    // If the current evaluation score is better for black (i.e. smaller) than the current min evaluation, then the min evaluation and its index are
                    // updated. The best black evaluation variable is then set to the lesser value of itself, and the current evaluation score.
                    if (evaluation.Item1 < minEvaluation) {
                        minEvaluation = evaluation.Item1;
                        minEvalIndex = i;
                        System.Diagnostics.Trace.WriteLine($"Min Value ({minEvaluation}), Index ({minEvalIndex})");
                    }
                    bestEvaluationBlack = Math.Min(bestEvaluationBlack, evaluation.Item1);

                    System.Diagnostics.Trace.WriteLine($"Returning {evaluation.Item1} ({moveList[minEvalIndex].MoveName()}) to L{depth}");

                    // If the best evaluation for black is less than (or equal) to the best evaluation for white, then the remaining of the branches
                    // of the move tree (at this depth) can be pruned, and thus are not evaluated.
                    if (bestEvaluationBlack <= bestEvaluationWhite) {
                        System.Diagnostics.Trace.WriteLine($"L{depth}/B{i + 1}-{moveList.Count} Pruned");
                        break;
                    }
                }

                return (minEvaluation, minEvalIndex);
            }
        }
    }
}

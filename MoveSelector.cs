using ChessEngineNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    // The purpose of this class is to search all possible sequences of moves on a given board, to a given move
    // depth and select the best possible move for the colour to play.
    public class MoveSelector
    {
        // The purpose of this function is to recursively search through all the moves and response moves on the 
        // chess board passed, to a given depth and evaluate the best possible move to make for the current colour 
// to play. It does this by creating a tree of moves, branches of which are 'pruned' for efficiency. Pruning 
// occurs when the current best possible evaluation for the black moves is less than the best possible evaluation of the white moves.

// NOTE: Best moves for black are indicated by (more) negative values, and best moves for white are indicated by (more) positive values.

	// This function returns an integer tuple, the first item is the best possible evaluation, and the second item is the 
       	// index of the move that causes this evaluation.
        public (int, int) MiniMaxMoveSearch(Board board, int depth, int bestEvaluationWhite, int bestEvaluationBlack)
        {
            // When the function reaches the bottom of the tree of possible moves (based on the defined depth)
            // it will return a "static evaluation" of the board, and sets the index of the move to -1.
            if (depth == 0 || board.Checkmate || board.Stalemate)
            {
                return (BoardEvaluator.Evaluate(board), -1);
            }

            // An instance of a move generator object is created, and is used to generate a list of all the legal moves in the 
            // current position.
            List<Move> moveList = board.LegalMoves;
            OrderMoves(moveList);

            if (board.ColourToPlay() == Piece.White)
            {
                // The Max evaluation is set to the smallest possible value, as this is the worst possible evaluation for white.
                int maxEvaluation = int.MinValue;
                int maxEvalIndex = -1;

// This loop iterates through the list of all legal moves, evaluating each move, and tracking the best possible move.
                for (int i = 0; i < moveList.Count; i++)
                {
                    // Get current move from move list.
                    Move boardMove = moveList[i];

                    // The current move is made on the board, so that this new position can be evaluated. Next, this function is 
                    // called recursively, passing in the new board position, and decrementing the depth indicator by one. Once 
                    // this function has returned, the current move is unmade, returning the board to it's original state.
                    board.MakeMove(boardMove);
                    (int, int) evaluation = MiniMaxMoveSearch(board, depth - 1, bestEvaluationWhite, bestEvaluationBlack);
                    board.UnmakeMove();
                 

                    // If the current evaluation score is better for white (i.e. greater) than current max evaluation, then the 
// max evaluation and its index are updated. The best white evaluation variable is then set to the greater // value of itself, and the current evaluation score.
                    if (evaluation.Item1 > maxEvaluation)
                    {
                        maxEvaluation = evaluation.Item1;
                        maxEvalIndex = i;
                    }
                    bestEvaluationWhite = Math.Max(bestEvaluationWhite, evaluation.Item1);


                    // If the best evaluation for black is less than (or equal) to the best evaluation for white, then the 
                    // remaining of the branches of the move tree (at this depth) can be pruned, and thus are not evaluated.
                    if (bestEvaluationBlack <= bestEvaluationWhite)
                    { 
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

// This loop iterates through the list of all legal moves, evaluating each move, and tracking the best possible move.
                for (int i = 0; i < moveList.Count; i++)
                {
                    // Get current move from move list.
                    Move boardMove = moveList[i];

                    // The current move is made on the board, so that this new position can be evaluated. Next, this function is 
                    // called recursively, passing in the new board position, and decrementing the depth indicator by one. Once 
                    // this function has returned, the current move is unmade, returning the board to it's original state.
                    board.MakeMove(boardMove);
                    (int, int) evaluation = MiniMaxMoveSearch(board, depth - 1, bestEvaluationWhite, bestEvaluationBlack);
                    board.UnmakeMove();

                    // If the current evaluation score is better for black (i.e. smaller) than the current min evaluation, then 
// the min evaluation and its index are updated. The best black evaluation variable is then set to the lesser value of itself, and the current evaluation score.
                    if (evaluation.Item1 < minEvaluation)
                    {
                        minEvaluation = evaluation.Item1;
                        minEvalIndex = i;
                    }
                    bestEvaluationBlack = Math.Min(bestEvaluationBlack, evaluation.Item1);

                    // If the best evaluation for black is less than (or equal) to the best evaluation for white, then the 
                    // remaining of the branches of the move tree (at this depth) can be pruned, and thus are not evaluated.
                    if (bestEvaluationBlack <= bestEvaluationWhite)
                    {
                        break;
                    }
                }

                return (minEvaluation, minEvalIndex);
            }
        }

        // This function takes a list of moves, and will sort them in order of how likely each move is to "be good". This 
        // function sorts the provided move list in place, and also returns the sorted list.
        private List<Move> OrderMoves(List<Move> moveList)
        {
            // Create an empty array of move "scores" that is the same length as the provided move-list.
            int[] moveOrderingScores = new int[moveList.Count]; 

            // Iterate over every move in the provided move-list and assign it a score based on how likely it is to be good.
            // The scores assigned to each move are smaller if they are more likely to be valuable moves.
            for (int i = 0; i < moveList.Count; i++)
            {
                // Create a reference to the current move in the move list for readability, and set it's default move priority
                // score to be 1000.
                Move move = moveList[i];
                int movePriority = 1000;

                // If this move captures a piece subtract the difference between the captured piece value, and the moving piece
                // that is capturing it. This operation results in capturing high-value pieces with low-value pieces (e.g. a 
                // pawn capturing a queen) being viewed as likely to be more valuable as there is low risk but high reward.
                if (move.Captured != Piece.Empty)
                { 
                    movePriority -= BoardEvaluator.PieceValues[move.Captured & Piece.Type] + BoardEvaluator.PieceValues[move.Piece & Piece.Type];
                }

                // Subtract the difference in the square-values of the start and end squares of the current move, for the moving 
 // piece, from the move score. Thus, a move that results in the moving piece ending on a better square is likely to be more valuable.
                movePriority -= BoardEvaluator.PieceSquareTables[move.Piece & Piece.Type][move.EndingSquare] - BoardEvaluator.PieceSquareTables[move.Piece & Piece.Type][move.StartingSquare];
               
                // Set the move ordering score at the current index, to be the calculated value.
                moveOrderingScores[i] = movePriority;
            }

// Perform a merge sort on the provided list of moves, and the move ordering scores array, sorting them in ascending order.
            MergeSort(moveList, moveOrderingScores, 0, moveList.Count - 1);

            return moveList;
        }

        public static void MergeSort(List<Move> moves, int[] scores, int left, int right)
        {
            // If the left pointer becomes equal to or greater than the right pointer, then the base case has been reached.
            if (left < right)
            {
                // Calculate the middle pointer of the array.
                int middle = (left + right) / 2;

                // Make two recursive calls of the left and right sides of the array, using the middle pointer
                // to split the main array(s) in half.
                MergeSort(moves, scores, left, middle);
                MergeSort(moves, scores, middle + 1, right);

                // Merge and sort the two halfs of the array.
                Merge(moves, scores, left, middle, right);
            }
        }

        private static void Merge(List<Move> moves, int[] scores, int left, int middle, int right)
        {
            // Find the sizes of the left and right sides of the array.
            int leftSize = middle - left + 1;
            int rightSize = right - middle;

            // Create new arrays/lists to store the left and right sides of the moves and scores lists/arrays.
            int[] leftScores = new int[leftSize];
            int[] rightScores = new int[rightSize];
            List<Move> leftMoves = new List<Move>();
            List<Move> rightMoves = new List<Move>();

            // Copy the left and right sides of both the scores and moves array/lists to their corresponding list.
            Array.Copy(scores, leftScores, leftSize);
            Array.Copy(scores, middle + 1, rightScores, 0, rightSize);
            leftMoves = moves.GetRange(left, leftSize);
            rightMoves = moves.GetRange(middle + 1, rightSize);

            // Define the indices that will be used to iterate over the arrays/lists
            // created above, when iterating over and merging them to the main array/list.
            int i = 0;
            int j = 0;
            int k = left;

            // Iterate through the left and right scores, and add them to the main array in sorted order.
            while (i < leftSize && j < rightSize)
            {
                // If the score in the left array at the current index is smaller than the
                // score in the right array at it's current index, then set the next element in the
                // main array/list to be the score/move at the current index in the left side arrays.
                if (leftScores[i] <= rightScores[j])
                {
                    scores[k] = leftScores[i];
                    moves[k] = leftMoves[i];

                    // Increment the left and main list indices.
                    i++; k++;
                }
                else
                {
                    // Otherwise, add the elements from the right-side lists to the main array/list.
                    scores[k] = rightScores[j];
                    moves[k] = rightMoves[j];

                    // Increment the right and main list indices.
                    j++; k++;
                }
            }

            // Add any remaining elements from the left-side lists to the main array.
            while (i < leftSize)
            {
                scores[k] = leftScores[i];
                moves[k] = leftMoves[i];

                i++; k++;
            }

            // Add any remaining elements from the right-side lists to the main array.
            while (j < rightSize)
            {
                scores[k] = rightScores[j];
                moves[k] = rightMoves[j];

                j++; k++;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace ChessEngineNS
{
    // An instance of this class will be used to generate moves for both players
    // in order to keep track of the friendly colours and whomever's turn it is to play.
    internal class MoveGenerator
    {    
        enum Directions
        {
            North = 8,
            NorthEast = 9,
            East = 1,
            SouthEast = -7,
            South = -8,
            SouthWest = -9,
            West = -1,
            NorthWest = 7
        }
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

        private int friendlyColour;
        private int opponentColour;
        private int friendlyListIndex;
        private int opponentListIndex;

        private bool whiteToPlay;

        private int kingPosition;

        private Board board;
        public MoveGenerator(Board board)
        {
            this.board = board;
            this.whiteToPlay = board.whiteToPlay;

            this.friendlyColour = whiteToPlay ? Piece.White : Piece.Black;
            this.opponentColour = whiteToPlay ? Piece.Black : Piece.White;

            friendlyListIndex = friendlyColour == Piece.White ? 0 : 1;
            opponentListIndex = opponentColour == Piece.White ? 0 : 1;

            this.kingPosition = board.Kings[friendlyListIndex];
        }

        private void Initialise(Board board)
        {
            this.board = board;
            this.whiteToPlay = board.whiteToPlay;

            this.friendlyColour = whiteToPlay ? Piece.White : Piece.Black;
            this.opponentColour = whiteToPlay ? Piece.Black : Piece.White;

            friendlyListIndex = friendlyColour == Piece.White ? 0 : 1;
            opponentListIndex = opponentColour == Piece.White ? 0 : 1;

            this.kingPosition = board.Kings[friendlyListIndex];
        }

        public List<Move> GenerateAllLegalMoves(Board board)
        {
            Initialise(board);

            int[] squares = this.board.GetBoard();
            CheckState checkState = DetectPinsAndCheck(squares, friendlyColour);

            if (checkState.NumOfChecks > 1)
            {
                return GenerateKingMoves(squares);
            }

            List<Move> pseudoLegalMoves = new List<Move>();

            for(int i = 0; i < squares.Length; i++)
            {
                if (squares[i] == Piece.Empty || (squares[i] & Piece.Colour) == opponentColour)
                    { continue; }

                if (Piece.IsKing(squares[i]))
                {
                    pseudoLegalMoves.AddRange(GenerateKingMoves(squares));
                }
                else if (Piece.IsSlidingPiece(squares[i]))
                {
                    pseudoLegalMoves.AddRange(GenerateSlideMoves(i, squares));
                }
                else if (Piece.IsKnight(squares[i]))
                {
                    pseudoLegalMoves.AddRange(GenerateKnightMoves(i, squares));
                }
                else if (Piece.IsPawn(squares[i]))
                {
                    pseudoLegalMoves.AddRange(GeneratePawnMoves(i, squares));
                }
            }

            return FilterLegalMoves(pseudoLegalMoves, checkState);
        }

        public List<Move> GenerateLegalMoves(int square, Board board)
        {
            // Initialise the board values.
            Initialise(board);

            int[] squares = this.board.GetBoard();
            int piece = board[square];
            
            List<Move> pseudoLegalMoves = new List<Move>();
            CheckState cState = DetectPinsAndCheck(squares, friendlyColour); // Find any checks or pins.

            // If there is more than one check on the king, only a king move will be able to get out of check. Therefore, if the piece
            // is not a king it has no legal moves.
            if ((cState.NumOfChecks > 1 && !Piece.IsKing(piece)) || piece == Piece.Empty)
                return pseudoLegalMoves;


            // Generate Pseudo-Legal Moves for the specified piece.
            if (Piece.IsKing(piece))
            {
                return GenerateKingMoves(squares);
            }   
            else if (Piece.IsKnight(piece))
            {
                pseudoLegalMoves = GenerateKnightMoves(square, squares);
            }
            else if (Piece.IsSlidingPiece(piece))
            {
                pseudoLegalMoves = GenerateSlideMoves(square, squares);
            }
            else if (Piece.IsPawn(piece))
            {
                pseudoLegalMoves = GeneratePawnMoves(square, squares);
            }
            

            // Once a list of all pseudo-legal moves has been created, all the non-legal moves must be filtered out from the fully legal ones.
            return FilterLegalMoves(pseudoLegalMoves, cState);
        }

        private List<Move> FilterLegalMoves(List<Move> pseudoLegalMoves, CheckState checkState)
        {
            List<Move> legalMoves = new List<Move>();

            if (pseudoLegalMoves.Count == 0)
            {
                return legalMoves;
            }

            // First look to see if there are any checks, which take precedent over pins.
            if (checkState.NumOfChecks == 1)
            {
                // If there is a check, but this piece is pinned, then this piece cannot prevent a check, so return an empty list.
                if (checkState.Pinned.Contains(pseudoLegalMoves[0].StartingSquare))
                {
                    return new List<Move>();
                }


                int checkerPosition = checkState.Checks[0];

                // Loop through all pseudo-legal moves.
                foreach (Move move in pseudoLegalMoves)
                {
                    // If this move lands on the checking piece, then it deals with the check and is valid.
                    if (move.EndingSquare == checkerPosition)
                    {
                        legalMoves.Add(move);

                        // If the piece is a queen then it will not be able to block the check, so any other moves will be invalid.
                        if (!Piece.IsQueen(board[move.StartingSquare]))
                        {
                            break;
                        }

                        continue;
                    }

                    // If the checking piece is a sliding piece, and the move lands on a square that will block the check, it is a legal square.
                    if (Piece.IsSlidingPiece(board[checkerPosition]) && IsMoveBlocking(checkerPosition, move.EndingSquare, kingPosition))
                    {
                        legalMoves.Add(move);
                    }
                }

            }
            else if (checkState.NumOfPins > 0) // If there is a pin.
            {
                int checkerPosition = -1;

                // Loop through all the pinned pieces, and check if this piece is pinned.
                for (int i = 0; i < checkState.NumOfPins; i++)
                {
                    if (checkState.Pinned[i] == pseudoLegalMoves[0].StartingSquare)
                    {
                        checkerPosition = checkState.Pins[i];
                    }
                }

                if (checkerPosition == -1) // This piece is not pinned.
                    return pseudoLegalMoves;

                // Loop through all pseudo-legal moves and check to see if the move either captures the pinner, or is within the pin-line.
                foreach (Move move in pseudoLegalMoves)
                {
                    if (move.EndingSquare == checkerPosition || IsMoveBlocking(checkerPosition, move.EndingSquare, kingPosition))
                    {
                        legalMoves.Add(move);
                    }
                }
            }
            else
            {
                // If there are no pins or checks then all moves are legal.
                legalMoves = pseudoLegalMoves;
            }

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

                        moves.Add(new Move(square, currentSquare, board[currentSquare]));

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

                        moves.Add(new Move(square, currentSquare, board[currentSquare]));

                        if ((board[currentSquare] & Piece.Colour) == opponentColour)
                            break;

                        currentSquare += direction;
                    }
                }
            }

            return moves;
        }

        public List<Move> GenerateKingMoves(int[] board)
        {
            List<Move> moves = new List<Move>();

            foreach (int direction in Enum.GetValues(typeof(Directions)))
            {
                int endSquare = kingPosition + direction;

                if (!Board.InBoard(endSquare))
                    continue;

                if(Board.File(kingPosition) == 7 && (direction == 1 || direction == 9 || direction == -7) ||
                   Board.File(kingPosition) == 0 && (direction == -1 || direction == -9 || direction == 7))
                {
                    continue;
                }

                // The purpose of this block of code is to ensure that the current move, would not put the moving king into check, 
                // as this is an illegal move.
                if ((board[endSquare] & Piece.Colour) == opponentColour || board[endSquare] == Piece.Empty)
                {
                    Move move = new Move(kingPosition, endSquare, board[endSquare]);

                    this.board.MakeMove(move, testMove: true);
                    kingPosition = endSquare;

                    if (DetectPinsAndCheck(this.board.GetBoard(), friendlyColour).NumOfChecks == 0)
                    {
                        moves.Add(move);
                    }

                    kingPosition = endSquare - direction;
                    this.board.UnmakeMove();
                } 
            }

            return moves;
        }

        public List<Move> GenerateKnightMoves(int square, int[] board)
        {
            List<Move> moves = new List<Move>();
            foreach(int direction in KnightMoves)
            {
                int endSquare = square + direction;

                // If move is outside board, disregard it.
                if (!Board.InBoard(endSquare))
                    continue;

                // Calculate the difference between the sum of each squares rank and file.
                int sqDiff = Math.Abs((Board.Rank(square) + Board.File(square)) - (Board.Rank(endSquare) + Board.File(endSquare)));
                
                // If this difference is not 1 or 3 then the move being made is out of range and is a wrap-around error.
                if (sqDiff > 3)
                    continue;

                // If the move is either empty, or an opponent piece then it is a valid move and can be added to the list.
                if ((board[endSquare] & Piece.Colour) == opponentColour || board[endSquare] == Piece.Empty)
                {
                    moves.Add(new Move(square, endSquare, board[endSquare]));
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
                    moves.Add(new Move(square, doubleFrontMove, board[doubleFrontMove]));
                }
                else if(Board.Rank(square) == 6 && Piece.IsBlack(board[square]) && board[doubleFrontMove] == Piece.Empty)
                {
                    moves.Add(new Move(square, doubleFrontMove));
                }
            }
            
            // Captures:
            int leftCapture = square + (direction * (int)DiagonalMoves.ForwardLeft);
            int rightCapture = square + (direction * (int)DiagonalMoves.ForwardRight);

            // The capture moves are valid if the move is within the board bounds, and if there is an opponent
            // piece residing on the target square.
            if (Board.InBoard(leftCapture) && (board[leftCapture] & Piece.Colour) == opponentColour && Board.AreAdjacentFiles(square, leftCapture))
            {
                moves.Add(new Move(square, leftCapture, board[leftCapture]));
            }

            if (Board.InBoard(rightCapture) && (board[rightCapture] & Piece.Colour) == opponentColour && Board.AreAdjacentFiles(square, rightCapture))
            {
                moves.Add(new Move(square, rightCapture, board[rightCapture]));
            }

            return moves;
        }

        public CheckState DetectPinsAndCheck(int[] board, int friendlyColour) 
        {
            List<int> checks = new List<int>();
            List<int> pins = new List<int>();
            List<int> pinned = new List<int>();

            int index = friendlyColour == this.friendlyColour ? friendlyListIndex : opponentListIndex;
            int kingPosition = this.board.Kings[index];

            int prevOpponentColour = opponentColour;

            foreach (int direction in Enum.GetValues(typeof(Directions)))
            {
                int endSquare = kingPosition + direction;
                int pinnedPiece = -1;

                bool orthogonal = (Board.IsSameRank(endSquare, kingPosition) || Board.IsSameFile(endSquare, kingPosition));

                while (Board.InBoard(endSquare))
                {
                    // Ensure that there is not a wrap-around error when searching the board in this direction, if there is then break out the loop.
                    if (orthogonal && !Board.IsSameRank(endSquare, kingPosition) && !Board.IsSameFile(endSquare, kingPosition))
                        break;
                    else if (!orthogonal && !Board.AreAdjacentRanks(endSquare, endSquare - direction))
                        break;

                    // If the piece is empty then continue to next iteration of the loop, since no pins nor checks can arise from this square.
                    if (board[endSquare] == Piece.Empty)
                    {
                        endSquare += direction;
                        continue;
                    }

                    // If the colour of the piece on the current square is the friendly colour, there is a possibility for a pin to the king.
                    if ((board[endSquare] & Piece.Colour) == friendlyColour)
                    {
                        // If there is already a pin in this direction, then there must be a piece between the current piece and the king,
                        // thus there is no pin on the king, and this direction does not need to be searched any further.
                        if (pinnedPiece == -1)
                        {
                            pinnedPiece = endSquare;

                            // Increment end square, and continue the loop.
                            endSquare += direction;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // One of the following conditions must be met for the current piece to be considered a threat to the king.
                    //      1. Is the current direction is ORTHOGONAL and is the piece a ROOK.
                    //      2. Is the current direction is DIAGONAL and is the piece a BISHOP.
                    //      3. Is the piece a QUEEN.
                    //      4. Is the piece a PAWN and is it ADJACENT to the KING. Is the pawn behind the king (perspectively).

                    if(orthogonal && Piece.IsRook(board[endSquare]) || 
                        !orthogonal && Piece.IsBishop(board[endSquare]) || 
                        Piece.IsQueen(board[endSquare]) || 
                        !orthogonal && Piece.IsPawn(board[endSquare]) && Board.AreAdjacentFiles(endSquare, kingPosition) &&
                        (Piece.IsWhite(board[endSquare]) && endSquare < kingPosition || Piece.IsBlack(board[endSquare]) && endSquare > kingPosition))
                    {

                        // If there is not a pinned piece, then this threat is considered a check. Otherwise it is a pin.
                        if (pinnedPiece == -1)
                        {
                            checks.Add(endSquare);
                        }
                        else
                        {
                            if (Piece.IsPawn(board[endSquare]))
                                break;

                            pins.Add(endSquare);
                            pinned.Add(pinnedPiece);
                        }

                    }
                    break; // No need to search further in this direction.
                }
            }

            opponentColour = friendlyColour == Piece.White ? Piece.Black : Piece.White;

            List <Move> knightMovesFromKing = GenerateKnightMoves(kingPosition, board);

            opponentColour = prevOpponentColour; // This may need to be altered.

            foreach(Move move in knightMovesFromKing)
            {
                if (Piece.IsKnight(board[move.EndingSquare]))
                {
                    checks.Add(move.EndingSquare);
                }
            }


            return new CheckState(checks.ToArray(), pins.ToArray(), pinned.ToArray());
        }

        // This function takes the indices of three seperate pieces, an attacking piece, the piece that is threatened, and a piece that intends to
        // block the attack. This function will return true if the blocking piece interrupts (blocks) the path of attack, and thus defends the threatened
        // piece, and false if it does not block the attack.
        private bool IsMoveBlocking(int attacker, int blocker, int threatened)
        {
            if (attacker == 36)
            {
                attacker = 36;
            }
            if (!((blocker > threatened && blocker < attacker) || (blocker < threatened && blocker > attacker)))
            {
                return false;
            }

            if ((Board.IsSameRank(threatened, attacker) && Board.IsSameRank(blocker, attacker) && Board.IsSameRank(blocker, threatened)) ||
                (Board.IsSameFile(threatened, attacker) && Board.IsSameFile(blocker, attacker) && Board.IsSameFile(blocker, threatened)))
            {
                return true;
            }

            if (Board.SameDiagonal(blocker, threatened) && Board.SameDiagonal(blocker, attacker) && Board.SameDiagonal(threatened, attacker))
            {
                return true;
            }

            return false;
        }


    }
}

/*
 7 x x x x x x x x
 6 x x x R x x x x
 5 x Q x x x x x x 
 4 x x x r x x x x
 3 x x x k x x x x
 2 x x x x x x x x
 1 x x x x x x x x
 0 x x x x x x x x
   0 1 2 3 4 5 6 7
 */
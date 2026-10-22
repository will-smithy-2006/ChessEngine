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
    // in order to keep track of the friendly colours and whoever’s turn it is to play.
    internal class MoveGenerator
    {

        // This enum represents the increments between adjacent squares in all directions.
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

        // This enum represents the increments needed to move from a square to any of it's
        // orthogonal neighbours.
        enum OrthogonalMoves
        {
            Forward = 8,
            Backward = -8,
            Left = -1,
            Right = 1
        }


        // This enum represents the increments needed to move from a square to any of it's
        // diagonal neighbours.
        enum DiagonalMoves
        {
            ForwardLeft = 7,
            ForwardRight = 9,
            BackwardLeft = -9,
            BackwardRight = -7,
        }

        // These are the increments for all the move's that a knight can make on the chess board.
        public int[] KnightMoves = { -15, 15, -17, 17, -10, 10, -6, 6 };

        // These piece-colour integer values represent the colour of pieces that should be considered
        // as allies or opponents when generating moves and captures.
        private int friendlyColour;
        private int opponentColour;

        // These integers represent the indexes used to access arrays that are dependant on a piece's colour
        // and dictate which list should be used for allie pieces, and which is for opponent pieces.
        private int friendlyListIndex;
        private int opponentListIndex;

        // This boolean indicates if white is the current colour to play.
        private bool whiteToPlay;

        // These variables represent the current colour to play's king's position and castling rights on the chess board.
        private int kingPosition;
        private bool[] castlingRights;

        // This is a copy of the board's current checkstate.
        private CheckState checkState;

        // This is a copy of the board object's current state.
        private Board board;


        // The class constructor takes a copy of a board object and initialises all the member variables above
        // depending on the current board state.
        public MoveGenerator(Board board)
        {
            castlingRights = new bool[2];
            Initialise(board);
        }


        // This function is called whenever move's on the board are to be generated, and the board state within this class must
        // be updated for the moves generated to be valid.
        private void Initialise(Board board)
        {
            // Make a copy of the board's current state, and update the white to play boolean.
            this.board = board;
            this.whiteToPlay = board.WhiteToPlay;

            // If white is to play, then the ally pieces are white, and enemy pieces are black.
            this.friendlyColour = whiteToPlay ? Piece.White : Piece.Black;
            this.opponentColour = whiteToPlay ? Piece.Black : Piece.White;

            // If the friendly colour is white, then uses the white index to access colour-sensitive arrays for ally pieces,
            // and if the opponent colour is white use this index to access the arrays for enemy pieces.
            friendlyListIndex = friendlyColour == Piece.White ? Board.WHITE_INDEX : Board.BLACK_INDEX;
            opponentListIndex = opponentColour == Piece.White ? Board.WHITE_INDEX : Board.BLACK_INDEX;

            // Find the position of the friendly king.
            this.kingPosition = board.Kings[friendlyListIndex];

            // Get the castling right's of the friendly king.
            castlingRights[Board.KINGSIDE_INDEX] = board.CastlingRights[friendlyListIndex, Board.KINGSIDE_INDEX];
            castlingRights[Board.QUEENSIDE_INDEX] = board.CastlingRights[friendlyListIndex, Board.QUEENSIDE_INDEX];

            // Get the current checkstate.
            checkState = board.CheckState;
        }

// This function takes a board object as an argument and will generate a list of all the legal moves for the colour to play
        // in the current position.
        public List<Move> GenerateAllLegalMoves(Board board)
        {
            // Update the board variables.
            Initialise(board);

            int[] squares = this.board.GetBoard();

            // If there is more than one check on the chess board, only a move from the king piece can resolve the check.
            // This means the function should return the king's legal moves, as there are no other ones available.
            if (checkState.NumOfChecks > 1)
            {
                return GenerateKingMoves(squares);
            }

            List<Move> moves = new List<Move>();

// Iterate over every square on the board, and for squares that contain pieces, generate the legal moves for the piece
            // on the square.
            for (int i = 0; i < squares.Length; i++)
            {
                if (squares[i] == Piece.Empty || (squares[i] & Piece.Colour) == opponentColour)
                { continue; }

                // Generate the pseudo-legal moves for the piece residing on the current square, and pass this
                // pseudo-legal move list into the FilterLegalMoves funcion, removing any moves that would leave the king
                // in check, and add the result from this function to the list of legal moves.
                if (Piece.IsKing(squares[i]))
                {
                    // Illegal king moves are filtered out at generation, so the filter legal moves
                    // function does not need to be called on the king's move list.
                    moves.AddRange(GenerateKingMoves(squares));
                }
                else if (Piece.IsSlidingPiece(squares[i]))
                {
                    moves.AddRange(FilterLegalMoves(GenerateSlideMoves(i, squares), checkState));
                }
                else if (Piece.IsKnight(squares[i]))
                {
                    moves.AddRange(FilterLegalMoves(GenerateKnightMoves(i, squares), checkState));
                }
                else if (Piece.IsPawn(squares[i]))
                {
                    moves.AddRange(FilterLegalMoves(GeneratePawnMoves(i, squares), checkState));
                }
            }

            // Return the list of legal moves.
            return moves;
        }

        // This function returns a list of legal moves for the piece residing on a given square, for a given board-layout.
        // This function is called from the board class by a pass-through function called from the Chess Form, whenever the 
        // user clicks on a piece to view and/or select a move.
        public List<Move> GenerateLegalMoves(int square, Board board)
        {
            // Initialise the board values.
            Initialise(board);

            int[] squares = this.board.GetBoard();
            int piece = board[square];

            List<Move> pseudoLegalMoves = new List<Move>();

// If there is more than one check on the king, only a king move will be able to get out of check. Therefore, if the piece
            // is not a king it has no legal moves.
            if ((checkState.NumOfChecks > 1 && !Piece.IsKing(piece)) || piece == Piece.Empty)
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
            return FilterLegalMoves(pseudoLegalMoves, checkState);
        }

// This function takes a list of pseudo-legal moves for a piece, removes and illegal moves and returns a new list of fully legal moves.
        private List<Move> FilterLegalMoves(List<Move> pseudoLegalMoves, CheckState checkState)
        {
            // If the pseudo-legal moves for a king piece are passed to this function, then return the list unaltered
            // since move kings are legalised at generation and do not need to be filtered.
            if (pseudoLegalMoves.Count > 0 && Piece.IsKing(pseudoLegalMoves[0].Piece))
            {
                return pseudoLegalMoves;
            }

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

// If the checking piece is a sliding piece, and the move lands on a square that will  block the check, it is a legal square.
                    if (Piece.IsSlidingPiece(board[checkerPosition]) && 
			 IsMoveBlocking(checkerPosition, move.EndingSquare, kingPosition))
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
                    if (move.EndingSquare == checkerPosition || 
			 IsMoveBlocking(checkerPosition, move.EndingSquare, kingPosition))
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
        private List<Move> GenerateSlideMoves(int square, int[] board)
        {
            List<Move> moves = new List<Move>();
            int pieceType = board[square] & Piece.Type;

            // If the piece residing on the given square is a rook or queen, generate all the pseudo-legal
            // orthogonal moves from this square (as this is allowed by the movement patterns of both pieces).
            if (pieceType == Piece.Rook || pieceType == Piece.Queen)
            {
                // Iterate over every direction (increment) within the orthogonal moves enum.
                foreach (int direction in Enum.GetValues(typeof(OrthogonalMoves)))
                {
                    // Calculate the next square in the current orthogonal direction.
                    int currentSquare = square + direction;

                    // While the next square remains within the board's bound keep moving in the
                    // current orthogonal direction.
                    while (Board.InBoard(currentSquare))
                    {
                        // If neither the rank nor file of the starting square and current square are the same, the piece must
                        // have travelled off the edge of the board, meaning a wrap-around error has occurred. This direction
                        // should stop being searchd.
                        if (!(Board.IsSameRank(square, currentSquare) || Board.IsSameFile(square, currentSquare)))
                            break;

                        // If the current square contains a friendly piece, the current direction should stop being searched
                        // as the moving piece may travel no futher.
                        if ((board[currentSquare] & Piece.Colour) == friendlyColour)
                            break;

// Create a new move object, using the starting square, current square, moving piece, and captured piece.
                        Move move = new Move(square, currentSquare, board[square], captured: board[currentSquare]);

// If the moving piece is a rook, then the current move has the potential to cause the colour to play's king
                        // to lose the right to castle to one side of the board. 
                        if (pieceType == Piece.Rook)
                        {
                            // If the rook is residing on the A file, and king currently has the right to queenside castle, 
                            // this move will cause the right to be lost, so add the corresponding flag, 
                            if (Board.File(square) == Files.A && castlingRights[Board.QUEENSIDE_INDEX])
                                move.AddFlag(Move.BreakQueensideFlag);
                            // Otherwise, if the rook is residing on the H file, and the king current has the right to kingside
                            // castle, this move will cause this right to be lost, so add the corresponding flag.
                            else if (Board.File(square) == Files.H && castlingRights[Board.KINGSIDE_INDEX])
                                move.AddFlag(Move.BreakKingsideFlag);
                        }

                        // Add this move to the moving piece's psuedo-legal move list.
                        moves.Add(move);

// If this move captures a piece, then this direction may be searched no further, so break out of the loop.
                        if ((board[currentSquare] & Piece.Colour) == opponentColour)
                            break;

                        // Otherwise, increment the square, and continue in the current direction.
                        currentSquare += direction;
                    }
                }
            }

            // If the piece residing on the given square is a rook or queen, generate all the pseudo-legal
            // diagonal moves from this square (as this is allowed by the movement patterns of both pieces).
            if (pieceType == Piece.Bishop || pieceType == Piece.Queen)
            {
                // Iterate over every direction (increment) in the diagonal moves enum.
                foreach (int direction in Enum.GetValues(typeof(DiagonalMoves)))
                {
                    // Calculate the next square in the current direction.
                    int currentSquare = square + direction;

                    // Search forward in the current direction, until the current square moves
                    // outside the board's bound.
                    while (Board.InBoard(currentSquare))
                    {
// If the ranks of the current square, and the square before it (in the current direction) are not adjacent
// the moving piece must have travelled off the edge of the board, causing a wrap-around error. Thus, this
                        // direction should be searched no further.
                        if (!Board.AreAdjacentRanks(currentSquare - direction, currentSquare))
                            break;

// If the current square contains a friendly piece, the moving piece may travel no further, so this direction
                        // should be searched no further.
                        if ((board[currentSquare] & Piece.Colour) == friendlyColour)
                            break;

                        // Add this pseudo-legal move to the move list.
                        moves.Add(new Move(square, currentSquare, board[square], captured: board[currentSquare]));

                        // If this move captures a piece, this direction may be searched no further, so exit the loop.
                        if ((board[currentSquare] & Piece.Colour) == opponentColour)
                            break;

                        // Otherwise, increment current square, and continue search in this direction.
                        currentSquare += direction;
                    }
                }
            }

            return moves;
        }

// This function generates and returns a list of legal moves that the current colour to play's king can make in the current
        // board position.
        private List<Move> GenerateKingMoves(int[] board)
        {
            List<Move> moves = new List<Move>();

            // Loop over all the directions (increments) in the directions enum.
            foreach (int direction in Enum.GetValues(typeof(Directions)))
            {
                int endSquare = kingPosition + direction;

                if (!Board.InBoard(endSquare))
                    continue;

// If the king is on the right-most edge of the board (File H) and the current direction will take it to the right
// (i.e. NorthEast, East, SouthEast) then the current direction will cause a wrap around error, so do not explore it.
// Similarly, if the king is on the left-most edge of the board (File A) and the current direction will take it to the
                // left (i.e. NorthWest, West, SouthWest) do not explore this direction, as it will cause a wrap-around error.
                if (Board.File(kingPosition) == 7 && (direction == 1 || direction == 9 || direction == -7) ||
                   Board.File(kingPosition) == 0 && (direction == -1 || direction == -9 || direction == 7))
                {
                    continue;
                }

 // The purpose of this block of code is to ensure that the current move, would not put the moving king into check, 
                // as this is an illegal move.
                if ((board[endSquare] & Piece.Colour) == opponentColour || board[endSquare] == Piece.Empty)
                {
                    Move move = new Move(kingPosition, endSquare, board[kingPosition], captured: board[endSquare]);

                    // The purpose of these two if statements is to test if a flag should be added to indicate that this move
                    // in particular causes the king to lose the right to castle kingside and/or queenside. It does this by 
  // looking at the kings castling rights before this move is made, and if it can castle, the corresponding flag is added.
                    if (castlingRights[Board.KINGSIDE_INDEX])
                        move.AddFlag(Move.BreakKingsideFlag);
                    if (castlingRights[Board.QUEENSIDE_INDEX])
                        move.AddFlag(Move.BreakQueensideFlag);

// Make the current move (marking it as a test move to save processing time), and update the king position variable.
                    this.board.MakeMove(move, testMove: true);
                    kingPosition = endSquare;

                    // If there are no checks currently on the board (for the moving king) add the move to list of legal king moves.
                    if (DetectPinsAndCheck(this.board.GetBoard(), friendlyColour).NumOfChecks == 0)
                    {
                        moves.Add(move);
                    }

                    // Reset the king position, and unmake the move.
                    kingPosition = endSquare - direction;
                    this.board.UnmakeMove();
                }
            }

            // If there are any checks present on the board, then return the legal move-list as the
            // king is not permitted to castle.
            if (checkState.NumOfChecks > 0)
                return moves;

            // If the current colour's king has the right to castle to it's kingside, then iterate over every square between 
            // itself and the rook located on the H file, to see if there are any pieces blocking the king from castling.
            if (castlingRights[Board.KINGSIDE_INDEX])
            {
for (int square = kingPosition + 1; square <= Board.FileRankToSquare(Files.H, Board.Rank(kingPosition)); square++)
                {
                    // If the current squrae is empty, continue to the next square.
                    if (board[square] == Piece.Empty)
                        continue;

                    // If any piece, other than a rook is encountered, then it is blocking the king's ability
                    // to castle kingside so break out of the loop, and search no further.
                    if (!Piece.IsRook(board[square]))
                        break;

  // Create a new move object with the king's starting positon, it's end positon, and the flag to indicate a kingside castle, and a 
                    // flag to indicate the loss of this right with this move.
                    Move move = new Move(kingPosition, kingPosition + 2, board[kingPosition], flags: Move.KingsideCastleFlag | Move.BreakKingsideFlag);

                    // If the king also has the right to queenside castle, add a flag to indicate the loss of this right with this move, otherwise
                    // add no such flag as the right has already been lost.
                    if (castlingRights[Board.QUEENSIDE_INDEX])
                        move.AddFlag(Move.BreakQueensideFlag);

                    // Make the castle move on the board (marking it as a test move) and like with regular king moves
                    // validate it does not cause any checks, and if it does not add it to the move list. Then unmake the move.
                    this.board.MakeMove(move, true);
                    kingPosition += 2;

                    if (DetectPinsAndCheck(this.board.GetBoard(), friendlyColour).NumOfChecks == 0)
                    {
                        moves.Add(move);
                    }

                    kingPosition -= 2;
                    this.board.UnmakeMove();
                }
            }

            // If the current colour's king has the right to castle to it's queenside, then iterate over every square between itself
            // and the rook located on the A file, to see if there are any pieces blocking the king from castling.
            if (castlingRights[Board.QUEENSIDE_INDEX])
            {
                // This loop follows the same algorithm as with the kinside castling, but searching in the opposite direction
                // and adding the queenside castling flags instead of the kingside one.
                for (int square = kingPosition - 1; square >= Board.FileRankToSquare(Files.A, Board.Rank(kingPosition)); square--)
                {
                    // Skip empty square.
                    if (board[square] == Piece.Empty)
                        continue;

                    // Break out of loop if piece is not rook.
                    if (!Piece.IsRook(board[square]))
                        break;

                    // Make new move object, add the correct corresponding flags.
                    Move move = new Move(kingPosition, kingPosition - 2, board[kingPosition], flags: Move.QueensideCastleFlag | Move.BreakQueensideFlag);

                    if (castlingRights[Board.KINGSIDE_INDEX])
                        move.AddFlag(Move.BreakKingsideFlag);

                    // Validate that this move does not cause a check, and consequently add it to the move list.
                    this.board.MakeMove(move, true);
                    kingPosition -= 2;

                    if (DetectPinsAndCheck(this.board.GetBoard(), friendlyColour).NumOfChecks == 0)
                    {
                        moves.Add(move);
                    }

                    kingPosition += 2;
                    this.board.UnmakeMove();
                }
            }

            // Return the list of legal king moves.
            return moves;
        }


        // This function generates and returns a list of pseudo-legal moves that a knight piece could be from a given square, in a given position.
        private List<Move> GenerateKnightMoves(int square, int[] board)
        {
            // Create empty list of pseudo-legal moves.
            List<Move> moves = new List<Move>();

            // Iterate over all the moves (increments) in the knight move arrays.
            foreach (int direction in KnightMoves)
            {
                int endSquare = square + direction;

                // If move is outside board, disregard it.
                if (!Board.InBoard(endSquare))
                    continue;

                // Calculate the difference between the sum of each squares rank and file.
                int sqDiff = Math.Abs((Board.Rank(square) + Board.File(square)) - (Board.Rank(endSquare) + Board.File(endSquare)));

                // If this difference is greater than 3 then the move being made is out of range and is a wrap-around error.
                if (sqDiff > 3)
                    continue;

                // If the move is either empty, or an opponent piece then it is a valid move and can be added to the list.
                if ((board[endSquare] & Piece.Colour) == opponentColour || board[endSquare] == Piece.Empty)
                {
                    moves.Add(new Move(square, endSquare, board[square], captured: board[endSquare]));
                }
            }

            // Return the list of pseudo-legal moves.
            return moves;
        }

        // This function generates and returns a list of pseudo-legal moves that a pawn on a given square
        // may make, in a given board position.
        private List<Move> GeneratePawnMoves(int square, int[] board)
        {
            List<Move> moves = new List<Move>();

            // Get a reference to the pawn piece for readabilty, and so it's colour can be determined, and hence the direction
            // that it moves in can be calculated. This direction multiplier will result flip the direction but not magnitude of a
            // pawn move (-1 for black, meaning downwards movement, +1 for white, meaning upwards movement).
            int piece = board[square];
            int direction = Piece.IsWhite(piece) ? 1 : -1;

            // Calculate the end square of the pawn's standard front move, and double front move.
            int frontMove = square + (direction * (int)OrthogonalMoves.Forward);
            int doubleFrontMove = frontMove + (direction * (int)OrthogonalMoves.Forward);

            // Calculate the flag's this move should have by determining if the front move will result in a promotion.
            int promotionFlag = Board.Rank(frontMove) == 0 || Board.Rank(frontMove) == 7 ? Move.PromoteToQueenFlag : 0;

            // First check to see if the front move is in the board, and then check to see if it is empty.
            if (Board.InBoard(frontMove) && board[frontMove] == Piece.Empty)
            {
                // If empty then the move is valid (pawn cannot take forwards).
                moves.Add(new Move(square, frontMove, board[square], flags: promotionFlag));

                // The front move must be valid in order for the double front move to be valid.
                // Check to see if the pawn has moved yet (pawns cannot move backwards), then check to see if the double move square is empty, if so the move is valid.
                if (Board.Rank(square) == 1 && Piece.IsWhite(board[square]) && board[doubleFrontMove] == Piece.Empty)
                {
                    moves.Add(new Move(square, doubleFrontMove, board[square]));
                }
                else if (Board.Rank(square) == 6 && Piece.IsBlack(board[square]) && board[doubleFrontMove] == Piece.Empty)
                {
                    moves.Add(new Move(square, doubleFrontMove, board[square]));
                }
            }

            // Calculate the end squares of the pawns capturing moves.
            int leftCapture = square + (direction * (int)DiagonalMoves.ForwardLeft);
            int rightCapture = square + (direction * (int)DiagonalMoves.ForwardRight);

            // The capture moves are valid if the move is within the board bounds, if there is an opponent
            // piece residing on the target square, and if the files of the capture square and pawn square are adjacent
            // indicating a wrap-around error has not occurred,
            if (Board.InBoard(leftCapture) && (board[leftCapture] & Piece.Colour) == opponentColour && Board.AreAdjacentFiles(square, leftCapture))
            {
                moves.Add(new Move(square, leftCapture, board[square], captured: board[leftCapture], flags: promotionFlag));
            }

            if (Board.InBoard(rightCapture) && (board[rightCapture] & Piece.Colour) == opponentColour && Board.AreAdjacentFiles(square, rightCapture))
            {
                moves.Add(new Move(square, rightCapture, board[square], captured: board[rightCapture], flags: promotionFlag));
            }

            // Return the list of pseudo-legal pawn moves.
            return moves;
        }

        // This function takes a board-array and a piece-colour integer value, and on this layout detects any squares where the
        // piece residing upon is creating a check on the given colour's king piece, detects the squares that contain pieces
        // pinned to this colour's king, and also records the squares of the pieces that are pinning these pieces. All this data
        // is returned within a custom struct called a "check state".
        public CheckState DetectPinsAndCheck(int[] board, int friendlyColour)
        {
            // Create three empty lists to store the squares of checking pieces, pinning pieces, and pinned pieces.
            List<int> checks = new List<int>();
            List<int> pins = new List<int>();
            List<int> pinned = new List<int>();

            // Calculate a list index, based on the friendly colour provided, and set the king position variable
            // to be this colour's king location.
            int index = friendlyColour == this.friendlyColour ? friendlyListIndex : opponentListIndex;
            int kingPosition = this.board.Kings[index];

            // Keep track of the opponent's colour (outside this function).
            int prevOpponentColour = opponentColour;

            // This algorithm operates using an I-SEE-YOU, YOU-SEE-ME principle, and will search out in every direction from the king
            // until it encounters other pieces (or reaches the edge of the board) and depending on the current direction, and the piece
            // on the current square, will record a check or pin on the current square.
            foreach (int direction in Enum.GetValues(typeof(Directions)))
            {
                // Calculate the next square in the current direction, and set the pinned piece variable a position of -1
                // (which is an invalid board indice, and thus indicated no pinned piece is present).
                int endSquare = kingPosition + direction;
                int pinnedPiece = -1;

                // This boolean indicates if the current direction being searched is an orthogonal or diagonal direction.
                bool orthogonal = direction == (int)OrthogonalMoves.Backward || direction == (int)OrthogonalMoves.Forward ||
                    direction == (int)OrthogonalMoves.Left || direction == (int)OrthogonalMoves.Right;

                // Search in the current direction until the edge of the board is reached.
                while (Board.InBoard(endSquare))
                {
                    // Ensure that there is not a wrap-around error when searching the board in this direction, if there is then break out the loop.
                    // (Wrap around errors for orthogonal and diagonal movement are explained in the GenerateSlideMoves function).
                    if (orthogonal && !Board.IsSameRank(endSquare, kingPosition) && !Board.IsSameFile(endSquare, kingPosition))
                        break;
                    else if (!orthogonal && !Board.AreAdjacentRanks(endSquare, endSquare - direction))
                        break;

                    // If the current square does not contain a piece, increment the square further in this direction, and continue
                    // to the next iteration of the loop, since no pins or checks will arise from this square.
                    if (board[endSquare] == Piece.Empty)
                    {
                        endSquare += direction;
                        continue;
                    }

                    // If the piece residing on the current square, is an ally, then no checks will arise from this square. However, there is a
                    // possibility that this piece may be pinned to the king.
                    if ((board[endSquare] & Piece.Colour) == friendlyColour)
                    {
                        // If there is already a pin in this direction, then there must be a piece between the current piece and the king,
                        // thus there is no pin on the king, and this direction does not need to be searched any further.

                        // If there is not already a (potential) pinned piece in the current direction, then the piece residing on the current square
                        // may be pinned to the king, so update the variable to show this. Otherwise, if there is already a (potential) pinned piece
                        // in this direction, then the neither this nor the other piece are pinned to the kin (since a pin may only occur across one friendly piece)
                        // so search no further in this direction.
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

                    // If the piece residing on the current square an opponent piece, then there is a possibility that is may pose a threat
                    // to the king, and either put the king in check, or apply a pin to a piece.

                    // One of the following conditions must be met for the current piece to be considered a threat to the king.
                    //      1. Is the current direction is ORTHOGONAL and is the piece a ROOK.
                    //      2. Is the current direction is DIAGONAL and is the piece a BISHOP.
                    //      3. Is the piece a QUEEN.
                    //      4. Is the piece a PAWN and is it ADJACENT to the KING and the direction is DIAGONAL. Is the pawn behind the king (perspectively).
                    //      5. Is the piece a KING and is it ADJACENT to the KING.

                    if (orthogonal && Piece.IsRook(board[endSquare]) ||
                        !orthogonal && Piece.IsBishop(board[endSquare]) ||
                        Piece.IsQueen(board[endSquare]) ||
                        !orthogonal && Piece.IsPawn(board[endSquare]) && Board.AreAdjacentFiles(endSquare, kingPosition) &&
                        (Piece.IsWhite(board[endSquare]) && endSquare < kingPosition || Piece.IsBlack(board[endSquare]) && endSquare > kingPosition) ||
                        (Piece.IsKing(board[endSquare]) && Board.AreAdjacent(kingPosition, endSquare)))
                    {

                        // If there is not a pinned piece, then this threat is considered a check. Otherwise it is a pin.
                        if (pinnedPiece == -1)
                        {
                            checks.Add(endSquare);
                        }
                        else
                        {
                            // Pawns cannot apply a pin to another piece.
                            if (Piece.IsPawn(board[endSquare]))
                                break;

                            pins.Add(endSquare);
                            pinned.Add(pinnedPiece);
                        }

                    }
                    break; // No need to search further in this direction.
                }
            }

            // Generate all the possible knight moves from the king's current position.
            opponentColour = friendlyColour == Piece.White ? Piece.Black : Piece.White;
            List<Move> knightMovesFromKing = GenerateKnightMoves(kingPosition, board);
            opponentColour = prevOpponentColour;

            // Any of the end squares of these moves that contain a knight piece, are considered
            // checks on the king (colour of the knight does not need to be checked since a knight
            // move to a square that contains a friendly piece).
            foreach (Move move in knightMovesFromKing)
            {
                if (Piece.IsKnight(board[move.EndingSquare]))
                {
                    checks.Add(move.EndingSquare);
                }
            }

            // Create and return a new checkstate struct, passing in the lists of checks, pins, and pinned pieces generated.
            return new CheckState(checks.ToArray(), pins.ToArray(), pinned.ToArray());
        }

        // This function takes the indices of three seperate pieces, an attacking piece, the piece that is threatened, and a piece that intends to
        // block the attack. This function will return true if the blocking piece interrupts (blocks) the path of attack, and thus defends the threatened
        // piece, and false if it does not block the attack.
        private bool IsMoveBlocking(int attacker, int blocker, int threatened)
        {
            // If the blocking piece is not located somewhere between the threatened piece and the attacking piece,
            // it cannot be blocking the attacker's threat to the threatened piece.
            if (!((blocker > threatened && blocker < attacker) || (blocker < threatened && blocker > attacker)))
            {
                return false;
            }

            // If the threatened and attacking pieces reside on the same file or rank, and the blocking piece also resides on the same rank
            // or file as BOTH these pieces, then it is blocking the attacker's threat to the threatened piece.
            if ((Board.IsSameRank(threatened, attacker) && Board.IsSameRank(blocker, attacker) && Board.IsSameRank(blocker, threatened)) ||
                (Board.IsSameFile(threatened, attacker) && Board.IsSameFile(blocker, attacker) && Board.IsSameFile(blocker, threatened)))
            {
                return true;
            }

            // If all three pieces ALL reside on the same diagonal, then the threatened piece must be blocking the attack on the threatened piece.
            if (Board.SameDiagonal(blocker, threatened) && Board.SameDiagonal(blocker, attacker) && Board.SameDiagonal(threatened, attacker))
            {
                return true;
            }

            // Otherwise, the blocking piece does not block the attack on the threatened piece.
            return false;
        }


    }
}

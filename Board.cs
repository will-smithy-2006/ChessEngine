using ChessEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    public class Board
    {
        // Default starting position fen string.
        public const string DEFAULT_FEN_STRING = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq";

        public const int WHITE_INDEX = 0;
        public const int BLACK_INDEX = 1;

        public const int KINGSIDE_INDEX = 0;
        public const int QUEENSIDE_INDEX = 1;

        // Keep track of king positions for easier check and pin detection.
        // kings[0] = White, kings[1] = Black
        public int[] Kings => kings;
        private int[] kings;

        public bool[,] CastlingRights => castlingRights;
        private bool[,] castlingRights;

        // These two booleans indicate whether the player is competing against another human player or against a computer. 
        public bool IsPlayingComputer => playingComputer;
        private bool playingComputer;

        // This boolean indicates if white is the current colour to play.
        public bool WhiteToPlay => whiteToPlay;
        private bool whiteToPlay;

        // This array of integers represents the current board state. Each element in the array represents a square of the board
        // and the particular piece that resides upon it, which is defined by the value within the piece class.
        public int this[int i] { get { return board[i]; } }
        private int[] board;

// This stack tracks all the moves that have been made in a game, since stacks are a LIFO data structure, the top of the stack
        // is the move most recently made, and the bottom is the first move made.
        private Stack<Move> moveStack;
        private Stack<List<Move>> legalMovesHistory;
        private Stack<CheckState> checkStates;

        public List<Move> LegalMoves => legalMoves;
        private List<Move> legalMoves;

        public CheckState CheckState => checkState;
        private CheckState checkState;

// These are instances of the Move Generator and Move Selector classes that are used throughout this class, for their own purposes.
        private MoveGenerator moveGenerator;
        private MoveSelector selector;

        // This boolean indicates whether a state of checkmate is present.
        public bool Checkmate => checkmate;
        private bool checkmate;

        // This boolean indicates whether a state of stalemate is present on the board.
        public bool Stalemate => stalemate;
        private bool stalemate;

        // The class constructor initialises all the variables above, with the board being set up as defined in the FEN String.
        public Board(string fenString = DEFAULT_FEN_STRING, bool playingComputer = false)
        {
            castlingRights = new bool[2, 2];
            board = GenerateBoardFromFENString(fenString);
            kings = new int[2];

            // Locate and record the King positions.
            for (int i = 0; i < 64; i++)
            {
                if (Piece.IsKing(board[i]))
                {
                    if (Piece.IsWhite(board[i]))
                        kings[WHITE_INDEX] = i;
                    else
                        kings[BLACK_INDEX] = i;
                }
            }

            moveGenerator = new MoveGenerator(this);
            moveStack = new Stack<Move>();
            checkStates = new Stack<CheckState>();
            selector = new MoveSelector();
            legalMovesHistory = new Stack<List<Move>>();


            checkmate = false;
            stalemate = false;
            this.playingComputer = playingComputer;

            checkState = moveGenerator.DetectPinsAndCheck(board, ColourToPlay());

            legalMoves = moveGenerator.GenerateAllLegalMoves(this);
        }

// This function takes a FEN string defining the board layout and populates an integer array representing the squares on the board.
// Furthermore, the function also determines the colour to play, and the castling rights of both kings as per the FEN string.
        private int[] GenerateBoardFromFENString(string fenString)
        {
            string[] fenStringParts = fenString.Split(' ');
            int[] board = new int[64];

            int file = 0;
            int rank = 7;

            foreach (char ch in fenStringParts[0])
            {
                if (ch == '/') { 
                    rank--;
                    file = 0;
                    continue;
                }

                if (Char.IsDigit(ch)) {
                    file += ch - '0';
                    continue;
                }

                int colour = (Char.IsUpper(ch)) ? Piece.White : Piece.Black;
                int piece;

                switch (Char.ToLower(ch))
                {
                    case 'p':
                        piece = Piece.Pawn;
                        break;
                    case 'b':
                        piece = Piece.Bishop;
                        break;
                    case 'n':
                        piece = Piece.Knight;
                        break;
                    case 'r':
                        piece = Piece.Rook;
                        break;
                    case 'q':
                        piece = Piece.Queen;
                        break;
                    case 'k':
                        piece = Piece.King;
                        break;
                    default:
			 throw new InvalidDataException($"Invalid FEN String Provided: '{fenString}' | Character: '{ch.ToString()}' is invalid");
                }

                board[(rank * 8) + file] = piece | colour;

                file++;
            }

            if (fenStringParts.Length < 2)
            {
                whiteToPlay = true;
                return board;
            }

            whiteToPlay = (fenStringParts[1] == "w") ? true : false;

            // Set the default castling rights to all be false.
            castlingRights = new bool[2, 2] { { false, false }, { false, false } };

            // If there are less than three fields provided within the fen string, it is a partial fen string,
// and does not contain information on the castling rights. Thus, the function can return the board, and exit the function.
            if (fenStringParts.Length < 3)
                return board;

            // Setup castling rights
            for (int i = 0; i < fenStringParts[2].Length; i++)
            {
                switch (fenStringParts[2][i])
                {
                    case 'K':
                        castlingRights[WHITE_INDEX, KINGSIDE_INDEX] = true;
                        break;
                    case 'k':
                        castlingRights[BLACK_INDEX, KINGSIDE_INDEX] = true;
                        break;
                    case 'Q':
                        castlingRights[WHITE_INDEX, QUEENSIDE_INDEX] = true; 
                        break;
                    case 'q':
                        castlingRights[BLACK_INDEX, QUEENSIDE_INDEX] = true;
                        break;
                    default:
                        break;
                }
            }

            return board;
        }

// This function takes an integer array representing all the squares on the board, and generates its corresponding FEN string.
        public string GenerateFENStringFromBoard(int[] board)
        {
            string fenString = "";

            for (int rank = 7; rank >= 0; rank--)
            {
                int emptySquares = 0;

                for (int file = 0; file < 8; file++)
                {
                    int piece = board[FileRankToSquare(file, rank)];

                    if (piece == Piece.Empty)
                    {
                        emptySquares++;

                        if (file == 7)
                        {
                            fenString += emptySquares.ToString();
                            emptySquares = 0;
                        }

                        continue;
                    }

                    if (emptySquares != 0)
                    {
                        fenString += emptySquares.ToString();
                        emptySquares = 0;
                    }

                    string ch = "";

                    switch(piece & Piece.Type)
                    {
                        case Piece.Pawn:
                            ch = "p";
                            break;
                        case Piece.Knight:
                            ch = "n";
                            break;
                        case Piece.Bishop:
                            ch = "b";
                            break;
                        case Piece.Rook:
                            ch = "r";
                            break;
                        case Piece.Queen:
                            ch = "q";
                            break;
                        case Piece.King:
                            ch = "k";
                            break;
                        default:
                            throw new InvalidDataException($"({piece & Piece.Type} is not a valid Piece Type");

                    }

                    if (Piece.IsWhite(piece))
                    {
                        ch = ch.ToUpper();
                    }

                    fenString += ch;
                }
                
                if (rank != 0)
                {
                    fenString += "/";
                }
            }

            fenString += ColourToPlay() == Piece.White ? " w" : " b";

            for (int col = WHITE_INDEX; col <= BLACK_INDEX; col++)
            {
                for (int side = KINGSIDE_INDEX; side <= BLACK_INDEX; side++)
                {
                    if (castlingRights[col, side])
                    {
                        string castle = side == KINGSIDE_INDEX ? "k" : "q";
                        fenString += (col == WHITE_INDEX) ? castle.ToUpper() : castle;
                    }
                }
            }

            return fenString;
        }

// This function takes a move object, and applies it to the board, updating any relevant board information, and ensuring that there is not an error
// while carrying out said move. The function will return true if the move was made successfully. This function takes a boolean value that when true indicates the move provided is only being assessed to establish it legality (within other // functions).
        public bool MakeMove(Move move, bool testMove = false)
        {
            // Create a variable and assign the piece that is moving on the board (for readability).
            int movingPiece = board[move.StartingSquare];
            
            // If the moving piece is an empty square, the move is invalid so return false.
            if (movingPiece == Piece.Empty)
                return false;

            // Get the corresponding list index for the colour of the moving piece.
            int colourIndex = Piece.IsWhite(movingPiece) ? WHITE_INDEX : BLACK_INDEX;

            // Set the starting square of the move to be empty, and the end square to be the moving piece.
            // This automatically captures a piece if one resides on the end square.
            board[move.StartingSquare] = Piece.Empty;

            if (move.PromoteToQueen)
                board[move.EndingSquare] = Piece.Queen | (movingPiece & Piece.Colour);
            else
                board[move.EndingSquare] = movingPiece;

            // If the moving piece is a king, then the king position variable is updated accordingly.
            if (Piece.IsKing(movingPiece))
            {
                kings[colourIndex] = move.EndingSquare;

                if (move.KingsideCastle || move.QueensideCastle)
                {
                    int rookStartingSquare = move.KingsideCastle ? FileRankToSquare(Files.H, Rank(move.StartingSquare)) : FileRankToSquare(Files.A, Rank(move.StartingSquare));
                    int rookEndSquare = move.KingsideCastle ? move.EndingSquare - 1 : move.EndingSquare + 1;

                    board[rookEndSquare] = board[rookStartingSquare];
                    board[rookStartingSquare] = Piece.Empty;
                }  
            }

            // If the current moves causes the king to lose its right to kingside and/or queenside castle, then update
            // the corresponding boolean in the castling rights array.
            if (move.BreakKingsideCastle)
                castlingRights[colourIndex, KINGSIDE_INDEX] = false;

            if (move.BreakQueensideCastle)
                castlingRights[colourIndex, QUEENSIDE_INDEX] = false;

            // Switch the colour to play.
            whiteToPlay = !whiteToPlay;

            // If the current move has been marked as a "test move" then the move will be pushed stack
            // and exit the function before looking for checkmate, to avoid recursive calling of this function. 
            if (testMove) 
            {
                moveStack.Push(move);
                legalMovesHistory.Push(legalMoves);
                checkStates.Push(checkState);
                return true;
            }

            // Push the current check state to the top of the check states stack.
            checkStates.Push(checkState);
            // Look for checks and pins on the (now) playing king.
            checkState = moveGenerator.DetectPinsAndCheck(board, ColourToPlay()); 
            
            // Add the current legal moves to the legal move stack, and then generate and update the legal
            // moves on the board.
            legalMovesHistory.Push(legalMoves);
            legalMoves = moveGenerator.GenerateAllLegalMoves(this);

            // If a check is present update move flag, and look for checkmate.
            if (checkState.NumOfChecks > 0)
            {
                // If there are no legal moves, then checkmate must be present.
                if (LegalMoves.Count == 0)
                {
                    checkmate = true;
                    move.AddFlag(Move.CheckmateFlag);
                }

                move.AddFlag(Move.CheckFlag);
            }
// If no check is present, but there are no legal moves on the board, the current colour to play's king is in stalemate
            // and the game is a draw.
            else if (legalMoves.Count == 0)
            {
                stalemate = true;
            }

            // Push the provided move, onto the move stack.
            moveStack.Push(move);

            return true;
        }

        // The purpose of this function is to "undo" the most recently made move on the board. 
        // The function will return true if the move was successfully unmade.
        public bool UnmakeMove()
        {
            // If the move stack is empty, return false, since there are no moves to unmake.
            if (moveStack.Count == 0)
            { return false; }
            
// If the legal move history stack is empty, return false, since there has been an error with the stacks.
            if (legalMovesHistory.Count == 0)
            { return false; }

            // If the check states stack is empty, return false, since there has been an error with the stacks.
            if (checkStates.Count == 0)
            { return false; }

            // Pop the most recent move from the move stack, and switch the colour to play.
// Since stacks are a LIFO (Last-In-First-Out) data structure, anytime a move is made and pushed to the stack, it will be the top element
// meaning that all moves must be unmade sequentially, from most recent to oldest, preventing the board from losing it's integrity when moves are
            // made and unmade.
            Move move = moveStack.Pop();
            whiteToPlay = !whiteToPlay;

// Calculate the index used to access colour-sensitive arrays, based on the colour of the piece being moved.
            int colourIndex = Piece.IsWhite(board[move.EndingSquare]) ? WHITE_INDEX : BLACK_INDEX;

            // If the moving piece is a king, update king position accordingly, and check if the current move
            // was a castling move.
            if (Piece.IsKing(board[move.EndingSquare]))
            {
                kings[colourIndex] = move.StartingSquare;

                // If the current move was a castke (to either side) calculate the starting and end squares for the rook (depending on which side was castled to)
                // and move the rook back to it's original position
                if (move.KingsideCastle || move.QueensideCastle)
                {
                    // Starting and end squares for when the castle move is being undone.
                    int rookStartingSquare = move.KingsideCastle ? move.EndingSquare - 1 : move.EndingSquare + 1;
                    int rookEndSquare = move.KingsideCastle ? FileRankToSquare(Files.H, Rank(move.StartingSquare)) : FileRankToSquare(Files.A, Rank(move.StartingSquare));

                    board[rookEndSquare] = board[rookStartingSquare];
                    board[rookStartingSquare] = Piece.Empty;

                }
            }

            // If the current moves caused the king to lose its right to kingside and/or queenside castle, then update
            // the corresponding boolean in the castling rights array.
            if (move.BreakKingsideCastle)
                castlingRights[colourIndex, KINGSIDE_INDEX] = true;

            if (move.BreakQueensideCastle)
                castlingRights[colourIndex, QUEENSIDE_INDEX] = true;

// Set the moving piece back to it's starting position, and replace any captured pieces, as indicated in the move object.
            board[move.StartingSquare] = move.Piece;
            board[move.EndingSquare] = move.Captured;

// Set checkmate to be false, no need to look if the move caused checkmate, since no moves may be made after checkmate has occurred.
            checkmate = false;
            stalemate = false;

            // Pop the top element of the legal move's history stack and assign this move list to be the current legal moves.
            // The same should be done with the check state's stack and the current check state.
            legalMoves = legalMovesHistory.Pop();
            checkState = checkStates.Pop();

            return true;
        }

        // This function takes an integer indicating the depth to which the move-tree should search, and will make the best
        // possible move for the current colour to play, in the current position. This function will return a boolean value
        // indicating if the move was successfully made.
        public bool MakeComputerMove(int depth)
        {
            // Call the MiniMaxMoveSearch algorithm which will search the move-tree of the current position to the
// given depth. Pass in the minimum integer value as the best score for white (since it must be overridden by any score that is better)
            // and the maximum integer value as the best score for black. (-ve is best for black, +ve is best for white).
            (int, int) selection = selector.MiniMaxMoveSearch(this, depth, int.MinValue, int.MaxValue);

            // If the index of the move to be made is -1 a move has not been selected so return false.
            // OR, if the index of the move to be made is an index greater than the number of current legal moves
            // there has been an error when using stacks to track the legal move history, so return false.
            if (selection.Item2 == -1 || selection.Item2 > legalMoves.Count - 1)
            { return false; }

            // Otherwise, make the selected move on the chess board.
            return MakeMove(legalMoves[selection.Item2]);
        }

// This function takes an integer indicating the depth to which the move-tree should be searched, and will return
// the best possible move for the colour to play, in the current position.
        public Move GenerateComputerMove(int depth)
        {
// Get the index of the best possible move to be made within the current position.
            int moveIndex = selector.MiniMaxMoveSearch(this, depth, int.MinValue, int.MaxValue).Item2;

            // Ensure the index returned is valid.
            if (moveIndex == -1 || moveIndex > legalMoves.Count - 1)
                throw new Exception("Index of -1 was returned from MoveSelection Algorithm");

            // If index is valid, return the selected move.
            return legalMoves[moveIndex];
        }

// This pass-through function will generate and return the legal moves for the piece residing on a given square.
        public List<Move> GenerateValidMoves(int square)
        {
            return moveGenerator.GenerateLegalMoves(square, this);
        }

// This function will generate and return a list of all the legal moves in the current position (for the colour to play).
        public List<Move> GenerateAllLegalMoves()
        {
            return moveGenerator.GenerateAllLegalMoves(this);
        }

// !!! NOTE: This is a test function and should not be used in any other capacity.
// This function returns the check-state in the current position of the current colour to play.
        public CheckState DetectPinsAndChecks() 
        {
            return moveGenerator.DetectPinsAndCheck(board, ColourToPlay());
        }

// This function returns the integer value (as defined in the piece module) for the current colour to play,
        public int ColourToPlay()
        {
            if (whiteToPlay)
                return Piece.White;
            return Piece.Black;
        }

        // This function returns the board array (board layout/position).
        public int[] GetBoard()
        {
            return board;
        }

        // Return the rank of a given square.
        public static int Rank(int square) {
            return square / 8;
        }

        // Return the file of a given square.
        public static int File(int square) {
            return square % 8;
        }

// This function returns the square number of a square with a given file and rank.
        public static int FileRankToSquare(int file, int rank)
        {
            return (rank * 8) + file;
        }
        public static bool InBoard(int square)
        {
            return square < 64 && square >= 0;
        }
        public static bool IsSameRank(int square, int otherSquare)
        {
            return Rank(square) == Rank(otherSquare);
        }
        public static bool IsSameFile(int square, int otherSquare)
        {
            return File(square) == File(otherSquare);
        }
        public static bool AreAdjacentFiles(int square, int otherSquare)
        {
            return Math.Abs(File(square) - File(otherSquare)) == 1;
        }
        public static bool AreAdjacentRanks(int square, int otherSquare)
        {
            return Math.Abs(Rank(square) - Rank(otherSquare)) == 1;
        }

        public static bool AreAdjacent(int square, int otherSquare)
        {
return AreAdjacentFiles(square, otherSquare) || AreAdjacentRanks(square, otherSquare);
        }

// This function returns a boolean value indicating if two squares reside upon the same diagonal.
        public static bool SameDiagonal(int square, int otherSquare)
        {
            // Calculate the absolute difference between the two squares.
            int squareDifference = Math.Abs(square - otherSquare);

// If this square difference is divisible by 7 or 9 (the difference between two adjacent diagonal squares), then the squares must
            // lie on the same diagonal, so return true.
            if (squareDifference % 7 == 0 || squareDifference % 9 == 0)
            {
                return true;
            }

            return false;
        }


// This function returns the name of a given square. A square name is it's file letter (a -> h) followed by
        // it's rank (1 -> 8).
        public static string SquareNumToName(int square)
        {
            string retString = "";

            switch (File(square))
            {
                case 0:
                    retString += "a";
                    break;
                case 1:
                    retString += "b";
                    break;
                case 2:
                    retString += "c";
                    break;
                case 3:
                    retString += "d";
                    break;
                case 4:
                    retString += "e";
                    break;
                case 5:
                    retString += "f";
                    break;
                case 6:
                    retString += "g";
                    break;
                case 7:
                    retString += "h";
                    break;
            }

            retString += $"{Rank(square) + 1}";
            return retString;
        }
    }
}

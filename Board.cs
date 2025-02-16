using ChessEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    public class Board
    {
        // Default starting position fen string.
        public const string DEFAULT_FEN_STRING = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w";

        // Keep track of king positions for easier check and pin detection.
        // kings[0] = White, kings[1] = Black
        public int[] Kings => kings;
        private int[] kings;

        // These two booleans indicate whether the player is competing against another human player or against a computer. 
        public bool IsPlayingComputer { get { return playingComputer; } }
        private bool playingComputer;

        // This boolean indicates if white is the current colour to play.
        public bool whiteToPlay;

        // This array of integers represents the current board state. Each element in the array represents a square of the board
        // and the particular piece that resides upon it, which is defined by the value within the piece class.
        public int this[int i] { get { return board[i]; } }
        private int[] board;

        // This stack tracks all the moves that have been made in a game, since stacks are a LIFO data structure, the top of the stack
        // is the move most recently made, and the bottom is the first move made.
        private Stack<Move> moveStack;

        // These are instances of the Move Generator and Move Selector classes that are used throughout this class, for their own purposes.
        private MoveGenerator moveGenerator;
        private MoveSelector selector;

        // This boolean indicates whether a state of checkmate is present.
        public bool Checkmate => checkmate;
        private bool checkmate;


        // The class constructor initialises all the variables above, with the board being set up as defined in the FEN String.
        public Board(string fenString = DEFAULT_FEN_STRING, bool playingComputer = false)
        {
            board = GenerateBoardFromFENString(fenString);
            kings = new int[2];

            // Locate and record the King positions.
            for (int i = 0; i < 64; i++)
            {
                if (Piece.IsKing(board[i]))
                {
                    if (Piece.IsWhite(board[i]))
                        kings[0] = i;
                    else
                        kings[1] = i;
                }
            }

            moveGenerator = new MoveGenerator(this);
            moveStack = new Stack<Move>();
            selector = new MoveSelector();

            checkmate = false;
            this.playingComputer = playingComputer;
        }

        // This function takes a FEN string defining the board layout and populates an integer array representing the squares on the board.
        // Furthermore, the function also determines the colour to play as per the FEN string.
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
                        throw new InvalidDataException($"FEN String: '{fenString}', character: '{ch.ToString()}'");
                }

                board[(rank * 8) + file] = piece | colour;

                file++;
            }

            whiteToPlay = (fenStringParts[1] == "w") ? true : false;
            
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
            return fenString;
        }

        // This function takes a move object, and applies it to the board, updating any relevant board information, and ensuring that there is not an error
        // while carrying out said move. The function will return true if the move was made successfully. This function takes a boolean value that when true
        // indicates the move provided is only being assessed to establish it legality (within other functions).
        public bool MakeMove(Move move, bool testMove = false)
        {
            // Create a variable and assign the piece that is moving on the board (for readability).
            int movingPiece = board[move.StartingSquare];
            
            // If the moving piece is an empty square, the move is invalid so return false.
            if (movingPiece == Piece.Empty)
                return false;

            // Set the starting square of the move to be empty, and the end square to be the moving piece.
            // This automatically captures a piece if one resides on the end square.
            board[move.StartingSquare] = Piece.Empty;
            board[move.EndingSquare] = movingPiece;

            // If the moving piece is a king, then the king position variable is updated accordingly.
            if (Piece.IsKing(movingPiece))
            {
                if (Piece.IsWhite(movingPiece))
                {
                    kings[0] = move.EndingSquare;
                }
                else
                {
                    kings[1] = move.EndingSquare;
                }
            }


            // Switch the colour to play.
            whiteToPlay = !whiteToPlay;

            // If the current move has been marked as a "test move" then the move will be pushed stack
            // and exit the function before looking for checkmate, to avoid recursive calling of this function. 
            if (testMove) 
            {
                moveStack.Push(move);
                return true;
            }

            // Look for checks and pins on the (now) playing king.
            CheckState checkState = moveGenerator.DetectPinsAndCheck(board, ColourToPlay()); 

            // If a check is present update move flag, and look for checkmate.
            if (checkState.NumOfChecks > 0)
            {
                // Generate a list of all legal moves on the board.
                List<Move> LegalMoves = moveGenerator.GenerateAllLegalMoves(this);

                // If there are no legal moves, then checkmate must be present.
                if (LegalMoves.Count == 0)
                {
                    checkmate = true;
                    move.AddFlag(Move.CheckmateFlag);
                }

                move.AddFlag(Move.CheckFlag);
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
            if (moveStack.Count == 0) return false;

            // Pop the most recent move from the move stack, and switch the colour to play.
            Move move = moveStack.Pop();
            whiteToPlay = !whiteToPlay;

            // If the moving piece is a king, update king position accordingly.
            if (Piece.IsKing(board[move.EndingSquare]))
            {
                if (Piece.IsWhite(board[move.EndingSquare]))
                    kings[0] = move.StartingSquare;
                else
                    kings[1] = move.StartingSquare;
            }

            // Set the moving piece back to it's starting position, and replace any captured pieces, as indicated in the move object.
            board[move.StartingSquare] = board[move.EndingSquare];
            board[move.EndingSquare] = move.Captured;

            // Set checkmate to be false, no need to look if the move caused checkmate, since no moves may be made after checkmate has occurred.
            checkmate = false;

            return true;
        }

        public bool MakeComputerMove()
        {
            (int, int) selection = selector.MiniMaxMoveEvaluation(this, 4, int.MinValue, int.MaxValue);

            if (selection.Item2 == -1)
                return false;

            List<Move> moves = moveGenerator.GenerateAllLegalMoves(this);

            return MakeMove(moves[selection.Item2]);
        }

        public List<Move> GenerateValidMoves(int square)
        {
            return moveGenerator.GenerateLegalMoves(square, this);
        }

        public CheckState DetectPinsAndChecks() // This is a test function and should not be used in any other capcity.
        {
            return moveGenerator.DetectPinsAndCheck(board, ColourToPlay());
        }
        public int ColourToPlay()
        {
            if (whiteToPlay)
                return Piece.White;
            return Piece.Black;
        }
        public int[] GetBoard()
        {
            return board;
        }

        public static int Rank(int square) {
            return square / 8;
        }
        public static int File(int square) {
            return square % 8;
        }
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

        public static bool SameDiagonal(int square, int otherSquare)
        {
            int squareDifference = Math.Abs(square - otherSquare);

            if (squareDifference % 7 == 0 || squareDifference % 9 == 0)
            {
                return true;
            }

            return false;
        }

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

/*
 * 56 57 58 59 60 61 62 63
 * 48 49 50 51 52 53 54 55
 * 40 41 42 43 44 45 46 47
 * 32 33 34 35 36 37 38 39
 * 24 25 26 27 28 29 30 31
 * 16 17 18 19 20 21 22 23
 * 08 09 10 11 12 13 14 15 
 * 00 01 02 03 04 05 06 07
 */

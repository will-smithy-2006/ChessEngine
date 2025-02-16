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
        public const string DEFAULT_FEN_STRING = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w";

        // Keep track of king positions for easier check and pin detection.
        // kings[0] = White, kings[1] = Black
        public int[] Kings => kings;
        private int[] kings;

        public bool IsPlayingComputer { get { return playingComputer; } }
        private bool playingComputer;
        public bool whiteToPlay;

        public int this[int i] { get { return board[i]; } }
        private int[] board;

        private Stack<Move> moveStack;
        private MoveGenerator moveGenerator;
        private Evaluator evaluator;

        public bool Checkmate => checkmate;
        private bool checkmate;


        public Board(string fenString = DEFAULT_FEN_STRING, bool playingComputer = false)
        {
            board = GenerateBoardFromFENString(fenString);
            kings = new int[2];

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
            evaluator = new Evaluator();

            checkmate = false;
            this.playingComputer = playingComputer;
        }

        public int[] GenerateBoardFromFENString(string fenString)
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

        // Returns true if move is successful, returns false otherwise. (e.g. move is invalid)
        public bool MakeMove(Move move)
        {
            int movingPiece = board[move.StartingSquare];
            
            if (movingPiece == Piece.Empty)
                return false;

            board[move.StartingSquare] = Piece.Empty;
            board[move.EndingSquare] = movingPiece;

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

            whiteToPlay = !whiteToPlay;

            if (Piece.IsKing(movingPiece)) // A King move cannot put the opposition king into check, or cause checkmate.
            {
                moveStack.Push(move);
                return true;
            }

            CheckState checkState = moveGenerator.DetectPinsAndCheck(board, ColourToPlay()); // Since kings cannot cause checks, there is no need to update the king position, within the move generator.

            if (checkState.NumOfChecks > 0)
            {
                List<Move> LegalMoves = moveGenerator.GenerateAllLegalMoves(this);

                if (LegalMoves.Count == 0)
                {
                    checkmate = true;
                }

                move.AddFlag(Move.CheckFlag);
            }

            moveStack.Push(move);
            return true;
        }

        public bool UnmakeMove()
        {
            if (moveStack.Count == 0) return false;

            Move move = moveStack.Pop();
            whiteToPlay = !whiteToPlay;

            if (Piece.IsKing(board[move.EndingSquare]))
            {
                if (Piece.IsWhite(board[move.EndingSquare]))
                    kings[0] = move.StartingSquare;
                else
                    kings[1] = move.StartingSquare;
            }

            board[move.StartingSquare] = board[move.EndingSquare];
            board[move.EndingSquare] = move.Captured;

            return true;
        }

        public bool MakeComputerMove()
        {
            (int, int) evaluation = evaluator.MiniMaxMoveEvaluation(this, 2, int.MinValue, int.MaxValue);
            //MessageBox.Show(evaluation.Item1.ToString());

            if (evaluation.Item2 == -1)
                return false;

            List<Move> moves = moveGenerator.GenerateAllLegalMoves(this);

            return MakeMove(moves[evaluation.Item2]);
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

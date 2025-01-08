using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    internal class Board
    {
        public const string DEFAULT_FEN_STRING = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w";

        int playerColour;
        int computerColour;

        public int FriendlyColour => playerColour;
        public int OpponentColour => computerColour;

        private MoveGenerator moveGenerator;

        public bool whiteToPlay;

        private int[] board;
        public int this[int i] { get { return board[i]; } } 



        public Board(string fenString = DEFAULT_FEN_STRING)
        {
            board = GenerateBoardFromFENString(fenString);
            moveGenerator = new MoveGenerator(this);
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

        private PieceList[] GeneratePieceListFromBoard(int pieceType, int[] board)
        {
            PieceList[] pieces = new PieceList[]
                {
                    new PieceList(),
                    new PieceList()
                };

            for (int i = 0; i < board.Length; i++)
            {
                if ((board[i] & Piece.Type) == pieceType)
                {
                    int colour = Piece.IsWhite(board[i]) ? 0 : 1;
                    pieces[colour].AddPiece(i);
                }
            }

            return pieces;
        }

        // Returns true if move is successful, returns false otherwise. (e.g. move is invalid)
        public bool MakeMove(Move move)
        {
            int movingPiece = board[move.StartingSquare];
            
            if (movingPiece == Piece.Empty)
                return false;

            board[move.StartingSquare] = Piece.Empty;
            board[move.EndingSquare] = movingPiece;

            whiteToPlay = !whiteToPlay;

            return true;
        }

        public List<Move> GenerateValidMoves(int square)
        {
            return moveGenerator.GenerateLegalMoves(square, this);
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

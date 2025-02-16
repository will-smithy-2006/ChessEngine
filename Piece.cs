using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    internal class Piece
    {
        public const int Empty = 0;  // 00000
        public const int Pawn = 1;   // 00001
        public const int Knight = 2; // 00011
        public const int Bishop = 3; // 00010
        public const int Rook = 4;   // 00100
        public const int Queen = 5;  // 00101
        public const int King = 6;   // 00110

        public const int White = 8;  // 01000
        public const int Black = 16; // 10000

        // Bitmasks
        public const int Type = 0b00111;  // (Piece AND Type) => PieceType
        public const int Colour = 0b11000; // (Piece AND Colour) => PieceColour

        public static string PieceName(int piece)
        {
            switch(piece & Type)
            {
                case Pawn:
                    return "Pawn";
                case Bishop:
                    return "Bishop";
                case Knight:
                    return "Knight";
                case Rook:
                    return "Rook";
                case Queen:
                    return "Queen";
                case King:
                    return "King";
                default:
                    throw new InvalidDataException($"Piece '{piece}' is invalid.");
            }
        }

        public static bool IsSlidingPiece(int piece) {
            return (piece & Type) >= Bishop && (piece & Type) <= Queen;
        }
        public static bool IsKing(int piece) => (piece & Type) == King;
        public static bool IsKnight(int piece) => (piece & Type) == Knight;
        public static bool IsPawn(int piece) => (piece & Type) == Pawn;

        public static bool IsRook(int piece) => (piece & Type) == Rook;
        public static bool IsBishop(int piece) => (piece & Type) == Bishop;
        public static bool IsQueen(int piece) => (piece & Type) == Queen;

        public static bool IsWhite(int piece) => (piece & Colour) == White;
        public static bool IsBlack(int piece) => (piece & Colour) == Black;
        public static string GetPieceImageID(int piece) { 
            return (IsWhite(piece)) ? $"w{PieceName(piece)}" : $"b{PieceName(piece)}";
        }



    }
}

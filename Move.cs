using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    public class Move
    {
        private int startingSquare;
        private int endSquare;
        private uint moveData; // 00 00 0 0 00000 00000

        // The following flags indicate various information about the move currenly being made. They are single-bit flags
        // and a 1 at their given position within the move data integer will indicate that they are present.
        public static readonly int CheckFlag = 1 << 10;
        public static readonly int CheckmateFlag = 1 << 11;

        public static readonly int KingsideCastleFlag = 1 << 12;
        public static readonly int QueensideCastleFlag = 1 << 13;
        public static readonly int BreakKingsideFlag = 1 << 14;
        public static readonly int BreakQueensideFlag = 1 << 15;

        public static readonly int PromoteToQueenFlag = 1 << 16;

        // These bitmasks isolate the moving and captured pieces within the move data integer.
        public static readonly int PieceBitmask = 0b0000011111;
        public static readonly int CapturedBitmask = 0b1111100000;

        // The following fields allow the basic information about a move object to be retrieved from
        // outside this class (ie. start squre, end square, moving piece, captured piece).
        public int StartingSquare => startingSquare;
        public int EndingSquare => endSquare;
        public int Piece => (int)(PieceBitmask & moveData);
        public int Captured => (int)((CapturedBitmask & moveData) >> 5);

        // The following boolean fields indicate whether the given flag is present within a given
        // move's move data integer.
        public bool Check => (CheckFlag & moveData) == CheckFlag;
        public bool Checkmate => (CheckmateFlag & moveData) == CheckmateFlag;
        public bool KingsideCastle => (KingsideCastleFlag & moveData) == KingsideCastleFlag;
        public bool QueensideCastle => (QueensideCastleFlag & moveData) == QueensideCastleFlag;
        public bool BreakKingsideCastle => (BreakKingsideFlag & moveData) == BreakKingsideFlag;
        public bool BreakQueensideCastle => (BreakQueensideFlag & moveData) == BreakQueensideFlag;
        public bool PromoteToQueen => (PromoteToQueenFlag & moveData) == PromoteToQueenFlag;

        // This class constructor initialises the member variables of this class. The captured piece of a move
        // is defaulted to be zero (an empty square) as are the move flags (indicating a move has no flags).
        public Move(int startingSquare, int endSquare, int piece, int captured = 0, int flags = 0)
        {
            this.startingSquare = startingSquare;
            this.endSquare = endSquare;
            
            // Initialise the move data integer, and store the given arguments within it
            // in their corresponding position, by applying a boolean OR operation.
            this.moveData = 0;
            this.moveData |= (uint)piece;
            this.moveData |= (uint)(captured << 5);
            this.moveData |= (uint)flags;
        }

        public bool AddFlag(int flag)
        {
            // If the flag is negative, or the flag is already present in the move data
            // return false as the adding of the flag cannot occur.
            if (flag < 0 || (flag & moveData) == flag) 
            {
                return false;            
            }

            // Apply binary OR operation to moveData and the flag, assign move data to be the result
            // of this operation.
            moveData |= (uint)flag;
            return true;
        }
        
        public bool RemoveFlag(int flag)
        {
            // If the flag is negative, or the flag is not present in the move data, return false since the flag
            // cannot be removed.
            if (flag < 0 || (moveData & flag) == 0)
            {
                return false;
            }

            // Perform a binary AND operation on the move data and the inverse of the flag
            // assign this result to the move data variable.
            moveData &= (ushort)~flag;
            return true;
        }

        public string MoveName()
        {
            return Board.SquareNumToName(startingSquare) + Board.SquareNumToName(endSquare);
        }
    }
}

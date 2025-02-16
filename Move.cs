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
        private ushort flags = 0; // 0000 0000 000 00 000

        // Flags
        public static int CheckFlag = 32;
        public static int CheckmateFlag = 64;

        // Bitmasks
        public static int CapturedBitmask = 0b011111;
        public static int CheckBitmask = 0b0100000;
        public static int CheckmateBitmask = 0b1000000;

        public int StartingSquare => startingSquare;
        public int EndingSquare => endSquare;

        public int Captured => CapturedBitmask & flags;
        public bool Checkmate => (CheckmateBitmask & flags) == CheckmateBitmask;

        public Move(int startingSquare, int endSquare, int flags = 0)
        {
            this.startingSquare = startingSquare;
            this.endSquare = endSquare;
            this.flags = (ushort)flags;
        }

        public bool AddFlag(int flag)
        {
            if (flag < 0 && flag > ushort.MaxValue) 
            {
                return false;            
            }

            flags |= (ushort)flag;
            return true;
        }
        
        public bool RemoveFlag(int flag)
        {
            if (flag < 0 && flag > ushort.MaxValue)
            {
                return false;
            }

            if ((flags & flag) == 0)
            {
                return false;
            }

            flags &= (ushort)~flag;
            return true;
        }

        public string MoveName()
        {
            return Board.SquareNumToName(startingSquare) + Board.SquareNumToName(endSquare);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public struct Move
    {
        private int startingSquare;
        private int endSquare;

        public int StartingSquare => startingSquare;
        public int EndingSquare => endSquare;

        public Move(int startingSquare, int endSquare)
        {
            this.startingSquare = startingSquare;
            this.endSquare = endSquare;
        }
    }
}

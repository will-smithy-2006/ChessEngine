using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    public struct CheckState
    {
        // This array stores the squares on the board that provide a check on the king.
        private int[] checks; 
        
        // This array stores the squares on the board that are pinning another piece to
        // the king.
        private int[] pins;

        // This array stores the squares on the board that contain pieces that are pinned
        // to the king.
        private int[] pinned;

        // The following fields allow for access to this struct's arrays.
        public int[] Checks { get { return checks; } }
        public int[] Pins { get { return pins; } }
        public int[] Pinned { get { return pinned; } }

        public readonly int NumOfChecks;
        public readonly int NumOfPins;

        // This constructor method initialises all three of this struct's arrays, with
        // the arrays provided as arguments.
        public CheckState(int[] checks, int[] pins, int[] pinned)
        {
            this.checks = checks;
            this.pins = pins;
            this.pinned = pinned;

            // Get the number of checks and pins from the length of checks and pins arrays.
            this.NumOfChecks = checks.Length;
            this.NumOfPins = pins.Length;
        }

    }
}

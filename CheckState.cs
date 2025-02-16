using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngineNS
{
    public struct CheckState
    {
        int[] checks;
        int[] pins;
        int[] pinned;
        public int[] Checks { get { return checks; } }
        public int[] Pins { get { return pins; } }
        public int[] Pinned { get { return pinned; } }

        public readonly int NumOfChecks;
        public readonly int NumOfPins;

        public CheckState(int[] checks, int[] pins, int[] pinned)
        {
            this.checks = checks;
            this.pins = pins;
            this.pinned = pinned;

            this.NumOfChecks = checks.Length;
            this.NumOfPins = pins.Length;
        }

    }
}

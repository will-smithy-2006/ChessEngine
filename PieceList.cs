using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    internal class PieceList
    {
        // This keeps track of all the position-indexes of this piece on the board.
        private int[] positions;
        public int[] Positions { get { return positions; } }

        // This keeps track of the position of the pieces in relation to the board indices.
        private int[] boardMap; 

        // Number of pieces, currently active on the board.
        private int count = 0;
        public int Count { get { return count; } }
        
        public PieceList(int maxNumPieces = 16)
        {
            positions = new int[maxNumPieces];
            boardMap = new int[64];

            // Sets the default value for the elements in the map to be -1, as 0 would be a valid index in the positions array,
            // and thus would be problematic as the default value

            for (int i = 0; i < 64; i++) {
                boardMap[i] = -1;
            }
        }
        public void AddPiece(int index)
        {
            positions[count++] = index;
            boardMap[index] = count - 1;
        }

        public bool RemovePiece(int index)
        {
            // Return true if the piece is successfully removed, return false if index given is not within array, or
            // if removal fails.

            // Check the board map to see if the index given as an argument is valid (if there is really a piece there).
            if (boardMap[index] == -1) {
                return false;
            }

            // Loop through the positions array, to remove the given index, and shift all the elements occuring afterwards
            // down one position, so that the array is continous, and decrement the corresponding indices in the board map.
            for (int i = boardMap[index]; i < count; i++)
            {
                positions[i] = positions[i + 1];
                boardMap[i]--;
            }

            // At the given index, set the board map value to the default (-1).
            boardMap[index] = -1;

            count--;

            return true;
        }
    }
}

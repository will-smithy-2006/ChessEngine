using ChessEngineNS;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace ChessTest
{
    [TestClass]
    public class ChessTests
    {
        [TestMethod]
        public void CheckTests()
        {
            string[] fenStringSetups = {"8/8/6p1/3p4/4K3/3P1p2/8/8 w", "8/2n1N3/1N3n2/3K4/1n3N2/2N1n3/8/8 w",
                                        "8/8/8/3K1b2/2B5/1b3b2/8/8 w", "8/8/4r3/1r1K4/8/3R4/3r4/8 w",
                                        "3q4/3Q4/5q2/3K4/8/1q1q4/8/8 w"};

            Dictionary<int, int[]> setupResultTable = new Dictionary<int, int[]>();

            setupResultTable[0] = new int[1] { 35 };
            setupResultTable[1] = new int[4] { 20, 25, 45, 50 };
            setupResultTable[2] = new int[1] { 21 };
            setupResultTable[3] = new int[1] { 33 };
            setupResultTable[4] = new int[2] { 17, 19 };

            // Loop over every fen string setup, generate the checks in this position, and compare
            // these to the expected result.
            for (int i = 0; i < fenStringSetups.Length; i++)
            {
                Board board = new Board(fenString: fenStringSetups[i]);
                int[] checks = board.DetectPinsAndChecks().Checks;

                Array.Sort(checks);

                CollectionAssert.AreEqual(setupResultTable[i], checks, $"Test #8.{i+1} - FAILED - ");
                Trace.WriteLine($"Test #8.{i + 1} - PASSED");
            }
        }

        [TestMethod]
        public void PinsTests()
        {
            string[] fenStringSetups = { "1b3r2/8/3B1R2/4B3/r1RR1K2/6B1/7b/8 w", "8/1q6/8/3Q4/q1Q1K3/8/8/8 w", "8/8/8/8/r1r1K3/8/8/8 w" };
            Dictionary<int, int[]> expectedPinsTable = new Dictionary<int, int[]>();
            expectedPinsTable[0] = new int[2] { 15, 61 };
            expectedPinsTable[1] = new int[2] { 24, 49 };
            expectedPinsTable[2] = new int[0] { };

            Dictionary<int, int[]> expectedPinnedTable = new Dictionary<int, int[]>();
            expectedPinnedTable[0] = new int[2] { 22, 45 };
            expectedPinnedTable[1] = new int[2] { 26, 35 };
            expectedPinnedTable[2] = new int[0] { };

            // Loop over every fen string setup, find the check state of this board.
            // Compare the pins and pinned arrays of this check state to the expected
            // pins and pinned arrays for this setup.
            for (int i = 0; i < fenStringSetups.Length; i++)
            {
                Board board = new Board(fenStringSetups[i]);
                CheckState checkState = board.DetectPinsAndChecks();

                int[] pins = checkState.Pins;
                int[] pinned = checkState.Pinned;

                Array.Sort(pins);
                Array.Sort(pinned);

                CollectionAssert.AreEqual(expectedPinsTable[i], pins, $"Test #9 (Setup {i + 1}) - (Pins) FAILED - ");
                Trace.WriteLine($"Test #9 (Setup {i + 1}) - (Pins) PASSED");
                CollectionAssert.AreEqual(expectedPinnedTable[i], pinned, $"Test #9 (Setup {i + 1}) - (Pinned) FAILED - ");
                Trace.WriteLine($"Test #9 (Setup {i + 1}) - (Pinned) PASSED");

            }

        }
        
        [TestMethod]  
        public void CheckmateTests()
        {
            string[] fenStringSetups = { "K7/6r1/7r/8/8/8/8/1k6 b", "7k/8/8/8/6n1/8/6PP/6RK b", 
                                         "2kr4/3p4/8/1B6/8/8/7B/7K w", "4K3/8/5q2/3b4/8/8/8/6k1 b" };

            Move[] moveSetups = { new Move(47, 63, 0), new Move(30, 13, 0), new Move(33, 40, 0), new Move(35, 42, 0) };

            // Loop over every fens string setup, make the corresponding move, and assert if the board is in a state
            // of checkmate.
            for (int i = 0; i < fenStringSetups.Length; i++)
            {
                Board board = new Board(fenString: fenStringSetups[i]);
                moveSetups[i].AddFlag(board[moveSetups[i].StartingSquare]);
                board.MakeMove(moveSetups[i]);

                Assert.IsTrue(board.Checkmate, $"Test #12 (Setup {i + 1}) - FAIL");
                Trace.WriteLine($"Test #12 (Setup {i + 1}) - PASS");
            }
        }

        [TestMethod]
        public void EvaluationTests()
        {
            string[] fenStringSetups = { Board.DEFAULT_FEN_STRING, "8/8/8/8/3Nq3/8/8/K6k w", "8/8/8/8/3Qn3/8/8/K6k w", "8/8/8/8/3Q3n/8/8/K6k w" };
            int[] expectedEvaluationScores = { 0, -550, 650, 700 };

// Loop over every fen string setup, and compare the expected evaluation score, to the current static evaluation of this board.
            for (int i = 0; i < fenStringSetups.Length; i++)
            {
                Board board = new Board(fenString: fenStringSetups[i]);
                Assert.AreEqual(expectedEvaluationScores[i], BoardEvaluator.Evaluate(board), $"Test #13 (Setup {i + 1}) - FAIL");
                Trace.WriteLine($"Test #13 (Setup {i + 1}) - PASS");
            }
        }

        [TestMethod]
        public void GenerateAllMovesTests()
        {
            string[] fenStringSetups = { "8/8/4p3/4P3/p4p2/2pN4/PP6/8 w", "8/8/8/p1p5/8/Qp6/3p4/R1B5 w", "8/8/8/8/8/8/K2r4/1r2r2k w" };

            string[][] expectedMoves = new string[3][];
            expectedMoves[0] = new string[] { "a2a3", "b2b3", "b2b4", "b2c3", "d3c5", "d3b4", "d3f2", "d3f4", "d3c1", "d3e1"};
            expectedMoves[1] = new string[] { "a1a2", "a1b1", "a3a4","a3a2", "a3a5", "a3c5", "a3b2", "a3b3", "a3b4", "c1b2", "c1d2" };
            expectedMoves[2] = new string[] { "a2a3" };

            // Loop over every fen string setup and get the legal moves within this position. Create a new array that contains 
// the move names of all the legal moves. Sort this array, and sort the array of expected move names. Compare these two arrays.
            for (int i = 0; i < fenStringSetups.Length; i++)
            {
                Board board = new Board(fenString: fenStringSetups[i]);
                List<Move> moves = board.LegalMoves;
                string[] moveNames= new string[moves.Count];
                
                for (int j = 0; j < moveNames.Length; j++)
                {
                    moveNames[j] = moves[j].MoveName();
                }

                Array.Sort(moveNames);
                Array.Sort(expectedMoves[i]);

                for (int j = 0; j < moveNames.Length; j++)
                {
                    Assert.AreEqual(expectedMoves[i][j], moveNames[j], $"Test #14.1 (Setup {i + 1}) - FAIL - (Items at Index ({j}) did not match)");
                }

                Trace.WriteLine($"Test #14.1 (Setup {i + 1}) - PASS");
            }
        }

        // This test method does not have a pass/fail state and was mainly used for debugging purposes.
        [TestMethod]
        public void MoveSearchingTests()
        {
            Board board = new Board(fenString: "n1P4b/8/1P3p2/8/8/8/8/k6K w", playingComputer: true);
            board.MakeMove(new Move(41, 49, board[41]));
            board.MakeMove(new Move(56, 41, board[56]));
            board.MakeMove(new Move(7, 15, board[7]));

            board.MakeComputerMove(5);
        }

        // This test method does not have a pass/fail state and was used to see the efficiency of the move searching 
        // algorithm before and after move ordering, and was used for testing other efficiencies.
        [TestMethod]
        public void TimedSearchingTests() 
        { 
            Board board = new Board(fenString: "r1bqkb1r/ppp2p1p/2np1np1/1B2p3/3PP3/2N2N2/PPPB1PPP/R2QK2R b", playingComputer: true);
   
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            board.MakeComputerMove(5);
            stopwatch.Stop();

            Trace.WriteLine($"Execution time: ({stopwatch.ElapsedMilliseconds}) ms");
        }

        
        [TestMethod]
        public void MergeSortTest()
        {
            // Create a new board with the following setup and get the list of legal moves.
            Board board = new Board("7k/3R4/8/8/2R2r2/8/8/K7 w");
            List<Move> moves = board.GenerateAllLegalMoves();
            // Create a new array of integers that represent each move's "score".
            Random random = new Random();
            int[] moveScores = new int[moves.Count];

            // Make these move scores completely random, and display them.
            Trace.Write("Unsorted Scores:");
            for (int i = 0; i  < moves.Count; i++)
            {
                moveScores[i] = random.Next(0, 1000);
                Trace.Write($" {moveScores[i]},");
            }

            // Display the corresponding moves for the move scores.
            Trace.Write("\nUnsorted Moves:");
            for(int i = 0; i < moves.Count; i++)
            {
                Trace.Write($" ({moves[i].MoveName()})");
            }
             
            // Sort the moves based on their corresponding move scores.
            Trace.WriteLine("\nSorting Moves based on corresponding Scores.");
            MoveSelector.MergeSort(moves, moveScores, 0, moveScores.Length - 1);

            // Display sorted scores.
            Trace.Write("Sorted Scores:");
            for (int i = 0; i < moves.Count; i++)
            {
                Trace.Write($" {moveScores[i]},");
            }

            // Display corresponding sorted moves.
            Trace.Write("\nSorted Moves:");
            for (int i = 0; i < moves.Count; i++)
            {
                Trace.Write($" ({moves[i].MoveName()})");
            }

            // Ensure that the move scores are in ascending order.
            for (int i = 1; i < moves.Count; i++)
            {
                if (moveScores[i] < moveScores[i - 1])
                    Assert.Fail("Test FAIL");
            }

            Trace.WriteLine("\n\nTest #15 - PASS");
        }

        [TestMethod]
        public void ComputerMoveTest()
        {
            string fenString = "n6b/1P6/5p2/8/8/8/8/k6K b";
            int depth = 2;
            int sequenceLength = 4;

            Board board = new Board(fenString);

            Trace.WriteLine($"FEN String: {fenString} | Depth: {depth} | Sequence Length: {sequenceLength}");

            for (int i = 0; i < sequenceLength; i++)
            {
                Trace.WriteLine($"\nMove {i + 1}/{sequenceLength}:");
                board.MakeComputerMove(depth);
            }
        }

        [TestMethod]
        public void UndoMoveTest()
        {
            // FEN string setups.
            string[] fenStringSetups = { "8/8/8/8/8/8/3P4/8 w", "q7/1P6/8/8/8/8/3K1k2/8 w", "2k5/8/8/8/8/8/8/R3K2R w KQ", "2k5/8/8/8/8/8/8/R3K2R w K" };
            string[] moveNames = { "d2d4", "b7b8", "e1g1", "a1a8" };

            for (int i = 0; i < fenStringSetups.Length; i++)
            {
                Board board = new Board(fenStringSetups[i]);
                Move moveToMake = new Move(-1, -1, -1);
                foreach (Move move in board.LegalMoves)
                {
                    if (move.MoveName() == moveNames[i])
                    {
                        moveToMake = move;
                    }
                }

                if (moveToMake.StartingSquare == -1)
                {
                    Assert.Fail($"T18 (Setup {i + 1}) - ERROR - The move with name {moveNames[i]} is not present within the Board's list of Legal Moves.");
                }

                // Record the board position and all the other associated board variables so that
                // they can be compared after the move to make has been made and unmade.
                int[] boardPosition = board.GetBoard();
                int[] kingPositions = board.Kings;
                bool[,] castlingRights = board.CastlingRights;
                bool playingComputer = board.IsPlayingComputer;
                int colourToPlay = board.ColourToPlay();
                bool whiteToPlay = board.WhiteToPlay;
                List<Move> moves = board.LegalMoves;
                int[] checks = board.CheckState.Checks;
                int[] pins = board.CheckState.Pins;
                int[] pinned = board.CheckState.Pinned;
                bool checkmate = board.Checkmate;
                bool stalemate = board.Stalemate;

                // Make and unmake the provided move.
                board.MakeMove(moveToMake);
                board.UnmakeMove();

                // Compare the current board state, with all the board state variables recorded.
                CollectionAssert.AreEqual(boardPosition, board.GetBoard(), $"T18 (Setup {i + 1}) - FAIL - Board layout's are not equal.");
                CollectionAssert.AreEqual(kingPositions, board.Kings, $"T18 (Setup {i + 1}) - FAIL - Recorded king position's are not equal.");
                CollectionAssert.AreEqual(castlingRights, board.CastlingRights, $"T18 (Setup {i + 1}) - FAIL - Castling Rights have changed");
                Assert.AreEqual(playingComputer, board.IsPlayingComputer, $"T18 (Setup {i + 1}) - FAIL - Playing Computer boolean has changed");
                Assert.AreEqual(colourToPlay, board.ColourToPlay(), $"T18 (Setup {i + 1}) - FAIL - Colour to play has switched");
                Assert.AreEqual(whiteToPlay, board.WhiteToPlay, $"T18 (Setup {i + 1}) - FAIL - White To Play boolean has changed");
                CollectionAssert.AreEqual(moves, board.LegalMoves, $"T18 (Setup {i + 1}) - FAIL - Legal Moves are different");
                CollectionAssert.AreEqual(checks, board.CheckState.Checks, $"T18 (Setup {i + 1}) - FAIL - Checks are different");
                CollectionAssert.AreEqual(pins, board.CheckState.Pins, $"T18 (Setup {i + 1}) - FAIL - Pins are different");
                CollectionAssert.AreEqual(pinned, board.CheckState.Pinned, $"T18 (Setup {i + 1}) - FAIL - Pinned pieces are different");
                Assert.AreEqual(checkmate, board.Checkmate, $"T18 (Setup {i + 1}) - FAIL - Checkmate status is different");
                Assert.AreEqual(stalemate, board.Stalemate, $"T18 (Setup {i + 1}) - FAIL - Stalemate status is different");

                Trace.WriteLine($"T18 (Setup {i + 1}) - PASS");
            }
        }
    }
}

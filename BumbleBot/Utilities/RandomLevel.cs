using System;

namespace BumbleBot.Utilities
{
    public class RandomLevel
    {
        public static readonly int RATIO_CHANCE_A = 75;

        public static readonly int RATIO_CHANCE_B = 15;

        //                         ...
        public static readonly int RATIO_CHANCE_N = 10;

        public static readonly int RATIO_TOTAL = RATIO_CHANCE_A
                                                 + RATIO_CHANCE_B
                                                 + RATIO_CHANCE_N;

        public static int GetRandomLevel()
        {
            var random = new Random();
            var x = random.Next(0, RATIO_TOTAL);

            if ((x -= RATIO_CHANCE_A) < 0) // Test for A
            {
                var randomLevel = new Random();
                return randomLevel.Next(75, 100);
            }

            if ((x -= RATIO_CHANCE_B) < 0) // Test for B
            {
                var randomLevel = new Random();
                return randomLevel.Next(25, 75);
            }
            else // No need for final if statement
            {
                var randomLevel = new Random();
                return randomLevel.Next(2, 25);
            }
        }
    }
}
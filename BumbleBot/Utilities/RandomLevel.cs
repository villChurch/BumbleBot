using System;

namespace BumbleBot.Utilities
{
    public class RandomLevel
    {
        public static readonly int RatioChanceA = 75;

        public static readonly int RatioChanceB = 15;

        //                         ...
        public static readonly int RatioChanceN = 10;

        public static readonly int RatioTotal = RatioChanceA
                                                 + RatioChanceB
                                                 + RatioChanceN;

        public static int GetRandomLevel()
        {
            var random = new Random();
            var x = random.Next(0, RatioTotal);

            if ((x -= RatioChanceA) < 0) // Test for A
            {
                var randomLevel = new Random();
                return randomLevel.Next(75, 100);
            }

            if ((x -= RatioChanceB) < 0) // Test for B
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
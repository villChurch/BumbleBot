using System;
namespace BumbleBot.Utilities
{
    public class RandomLevel
    {
        public RandomLevel()
        {
        }
        public static readonly int RATIO_CHANCE_A = 10;
        public static readonly int RATIO_CHANCE_B = 30;
        //                         ...
        public static readonly int RATIO_CHANCE_N = 60;

        public static readonly int RATIO_TOTAL = RATIO_CHANCE_A
                                               + RATIO_CHANCE_B
                                               + RATIO_CHANCE_N;
        public static int GetRandomLevel()
        {
            Random random = new Random();
            int x = random.Next(0, RATIO_TOTAL);

            if ((x -= RATIO_CHANCE_A) < 0) // Test for A
            {
                Random randomLevel = new Random();
                return randomLevel.Next(75, 100);
            }
            else if ((x -= RATIO_CHANCE_B) < 0) // Test for B
            {
                Random randomLevel = new Random();
                return randomLevel.Next(25, 75);
            }
            else // No need for final if statement
            {
                Random randomLevel = new Random();
                return randomLevel.Next(0, 25);
            }
        }
    }
}

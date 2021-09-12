using System;
using System.Collections.Generic;

namespace lab_02.Utils
{
    public static class MathHelper
    {
        public static double Median(double[] data)
        {
            Array.Sort(data);
            if (data.Length % 2 == 0)
                return (data[data.Length / 2 - 1] + data[data.Length / 2]) / 2;
            return data[data.Length / 2];
        }
    }
}
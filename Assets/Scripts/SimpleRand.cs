using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SimpleRand
{
    public const int RandMax = 32767;

    public int Seed;

    public int Next()
    {
        unchecked
        {
            Seed = Seed * 0x343FD + 0x269EC3;
            return (Seed >> 0x10) & 0x7FFF;
        }
    }

    public float NextFloat()
    {
        return ((float)Next()) / ((float)RandMax);
    }
}
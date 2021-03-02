using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class OctaveGenerator
{
    public float Power { get; private set; }
    public float Zoom { get; private set; }

    private float xo;
    private float yo;

    public OctaveGenerator(int seed, float power, float zoom)
    {
        var rnd = new System.Random(seed ^ 7138923);
        xo = (float)rnd.Next(-1000, 1000) + (float)rnd.NextDouble();
        yo = (float)rnd.Next(-1000, 1000) + (float)rnd.NextDouble();
        this.Power = power;
        this.Zoom = zoom;
    }

    public float Sample(float x, float y)
    {
        return Mathf.Clamp01(Mathf.PerlinNoise((x + xo) * Zoom, (y + yo) * Zoom)) * Power;
    }
}

public class MultiOctaveGenerator : List<OctaveGenerator>
{
    private float offset = 0f;

    public MultiOctaveGenerator(int seed, int octaveCount, float zoom)
    {
        var rnd = new System.Random(seed ^ 41789039);

        float power = .5f;
        for (int i = 0; i < octaveCount; i++)
        {
            Add(new OctaveGenerator(rnd.Next(), power, zoom));

            // the noiser it gets, the less it will affect the overall shape of things.
            power /= 2.0f;
            zoom *= 2.0f;
        }

        offset = power;
    }

    public float Sample(float x, float y) => Mathf.Clamp01(this.Select(_ => _.Sample(x, y)).Sum() + offset);
}
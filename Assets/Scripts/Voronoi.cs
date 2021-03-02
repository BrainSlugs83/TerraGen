using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleVoronoi
{
    public float Zoom { get; private set; } = 200f;
    public int Seed { get; private set; }

    public SimpleVoronoi(int seed, float zoom)
    {
        this.Seed = seed;
        this.Zoom = zoom;
    }

    private int GetCellIdx(float v) => Mathf.FloorToInt(v);

    private int GetId(int cx, int cy)
    {
        unchecked
        {
            return ((cx + 378102) ^ (cy - 9032185) * 39021) - 642178;
        }
    }

    private Dictionary<int, Vector2> cache = new Dictionary<int, Vector2>();

    private SimpleRand sr = new SimpleRand();

    private Vector2 CellPos(int cx, int cy)
    {
        var id = GetId(cx, cy);
        if (cache.ContainsKey(id)) { return cache[id]; }

        sr.Seed = id ^ Seed;
        return cache[id] = new Vector2
        (
            cx + sr.NextFloat(),
            cy + sr.NextFloat()
        );
    }

    public float Sample(float x, float y)
    {
        x *= Zoom;
        y *= Zoom;

        int cx = GetCellIdx(x);
        int cy = GetCellIdx(y);

        var lpos = new Vector2(x, y);
        var closestPos = Vector2.zero;
        float minDist = float.MaxValue; // force it to take the first value we compare.

        for (int nx = -1; nx <= 1; nx++)
        {
            for (int ny = -1; ny <= 1; ny++)
            {
                var pos = CellPos(cx + nx, cy + ny);
                var sqrMag = (pos - lpos).sqrMagnitude;
                if (sqrMag < minDist)
                {
                    closestPos = pos;
                    minDist = sqrMag;
                }
            }
        }

        return (lpos - closestPos).magnitude;
    }
}
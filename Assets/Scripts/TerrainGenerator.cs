using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

//[RequireComponent(typeof(MeshRenderer))]
//[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(Terrain))]
[RequireComponent(typeof(TerrainCollider))]
[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    /*public ComputeShader TerrainCompute;
    private RenderTexture ComputeTexture;*/

    public int Resolution = 256;
    public int Height = 70;
    public float Zoom = 6f;

    public int OctaveCount = 4;
    public string Seed = "COOK IT";

    public float StepCount = 25f;

    public int SmoothingSteps = 2;

    public bool invert = false;

    public bool fallOff = false;

    public Vector2 VoronoiClamp = new Vector2(0.0f, 1.0f);

    private Terrain terrain;
    private MultiOctaveGenerator octGen;
    private SimpleVoronoi vorGen;

    private bool needsUpdate = false;

    //// Start is called before the first frame update
    //private void Awake()
    //{
    //    terrain = GetComponent<Terrain>();
    //    //AllocateIfNecessary();
    //}

    public void Start()
    {
        //AllocateIfNecessary();
        RebuildTerrain();
    }

    //private void OnDisable()
    //{
    //    Release();
    //}

    //private void AllocateIfNecessary()
    //{
    //    if (TerrainCompute && SystemInfo.supportsComputeShaders)
    //    {
    //        if (ComputeTexture == null || ComputeTexture.width != Resolution || ComputeTexture.height != Resolution)
    //        {
    //            Release();
    //            ComputeTexture = new RenderTexture(Resolution, Resolution, 32, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
    //            ComputeTexture.enableRandomWrite = true;
    //            ComputeTexture.useMipMap = false;
    //            ComputeTexture.Create();
    //            DispatchGpu();
    //        }
    //    }
    //    else
    //    {
    //        Release();
    //    }
    //}

    //private void Release()
    //{
    //    if (ComputeTexture != null)
    //    {
    //        ComputeTexture.Release();
    //        ComputeTexture = null;
    //    }
    //}

    private void OnValidate()
    {
        if (OctaveCount < 1) { OctaveCount = 1; }
        if (Zoom < .001f) { Zoom = .001f; }
        if (Resolution > 1024) { Resolution = 1024; }
        
        needsUpdate = true;
    }

    private void RebuildTerrain()
    {
        if (!terrain) { terrain = GetComponent<Terrain>(); }

        //if (TerrainCompute && SystemInfo.supportsComputeShaders)
        //{
        //    AllocateIfNecessary();
        //    return;
        //}

        OctaveCount = Mathf.Clamp(OctaveCount, 1, 100);
        GenerateTerrain(terrain.terrainData);
    }

    private void GenerateTerrain(TerrainData input)
    {
        input.heightmapResolution = Resolution + 1;
        input.size = new Vector3(Resolution, Height, Resolution);

        octGen = new MultiOctaveGenerator(Seed.GetHashCode() ^ "PERLIN".GetHashCode(), OctaveCount, Zoom);
        vorGen = new SimpleVoronoi(Seed.GetHashCode() ^ "VORONOI".GetHashCode(), Zoom);
        input.SetHeights(0, 0, GenerateHeights((int)input.size.x, (int)input.size.z));
    }

    private float[,] GenerateHeights(int mx, int mz)
    {
        float[,] heights = new float[mz, mx];

        for (int x = 0; x < mx; x++)
        {
            for (int z = 0; z < mz; z++)
            {
                heights[z, x] = Sample(x, z);
            }
        }

        for (int i = 0; i < SmoothingSteps; i++)
        {
            for (int x = 0; x < mx; x++)
            {
                for (int z = 0; z < mz; z++)
                {
                    float avg = 0f;
                    float count = 0f;

                    for (int xo = -1; xo <= 1; xo++)
                    {
                        for (int zo = -1; zo <= 1; zo++)
                        {
                            var x2 = x + xo;
                            var z2 = z + zo;

                            if (x2 >= 0 && x2 < heights.GetLength(0) && z2 >= 0 && z2 < heights.GetLength(1))
                            {
                                avg += heights[z2, x2];
                                count++;
                            }
                        }
                    }

                    avg += (heights[z, x] * 3f);
                    count += 3f;

                    heights[z, x] = avg / count;
                }
            }
        }

        if (fallOff)
        {
            for (int x = 0; x < Resolution; x++)
            {
                for (int z = 0; z < Resolution; z++)
                {
                    float u = (x / (float)Resolution) * 2.0f - 1.0f;
                    float v = (z / (float)Resolution) * 2.0f - 1.0f;
                    var fo = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                    if (fo >= .5f)
                    {
                        var ifov = 1.0f / 16.0f;
                        var fov = 1.0f - ifov;
                        fo -= fov;
                        if (fo >= 0f)
                        {
                            fo = 1.0f - ((fo / ifov) * (fo / ifov));
                            heights[z, x] *= fo;
                        }
                    }
                }
            }
        }

        return heights;
    }

    private float Sample(float x, float z)
    {
        x += transform.position.x;
        z += transform.position.z;

        float xCoord = ((x / Resolution) * Zoom) * .1f;
        float zCoord = ((z / Resolution) * Zoom) * .1f;

        //var v = Mathf.Clamp01(vorGen.Sample(xCoord, zCoord));
        //var weight = 1.0f - Mathf.Clamp01(Mathf.Lerp(0.25f, 0.9f, (v * v) / .5f));

        var weight = vorGen.Sample(xCoord, zCoord);
        if (VoronoiClamp.x < VoronoiClamp.y)
        {
            weight = Mathf.Clamp(weight, VoronoiClamp.x, VoronoiClamp.y);
            weight = Mathf.SmoothStep(0f, 1f, (weight - VoronoiClamp.x) / (VoronoiClamp.y - VoronoiClamp.x));
        }

        //weight = (weight + .5f) / 2f;

        var result = Mathf.SmoothStep(0f, 1f, weight * octGen.Sample(xCoord, zCoord));

        if (StepCount > 0f)
        {
            result = Mathf.Round(result * StepCount);

            //var min = StepCount * .4f;
            //var max = StepCount * .6f;

            //if (result > min && result < max)
            //{
            //    result = StepCount * .5f;
            //}

            result /= StepCount;
        }

        if (invert) { result = 1.0f - result; }

        return result;

        //return Mathf.PerlinNoise(xCoord, zCoord);

        /*return ((Mathf.Cos(xCoord) * .5f + .5f)
            + (Mathf.Sin(zCoord) * .5f + .5f)) / 2.0f;*/
    }

    private Vector2 oldPos;

    public void Update()
    {
        if (needsUpdate || oldPos.x != transform.position.x || oldPos.y != transform.position.z)
        {
            OnValidate();
            RebuildTerrain();
            oldPos = new Vector2(transform.position.x, transform.position.z);
            needsUpdate = false;
        }
    }

    //public void Update()
    //{
    //    if (ComputeTexture != null && terrain && terrain.terrainData)
    //    {
    //        terrain.terrainData.heightmapResolution = Resolution + 1;
    //        terrain.terrainData.size = new Vector3(Resolution, Height, Resolution);

    //        var texture = new Texture2D(ComputeTexture.width, ComputeTexture.height, TextureFormat.RFloat, 0, true);

    //        RenderTexture.active = ComputeTexture;
    //        texture.ReadPixels(new Rect(0, 0, ComputeTexture.width, ComputeTexture.height), 0, 0, false);
    //        texture.Apply();
    //        RenderTexture.active = null;

    //        var heights = new float[Resolution, Resolution];

    //        using (var rawHeights = texture.GetPixelData<float>(0))
    //        {
    //            int i = 0;

    //            for (int x = 0; x < texture.width; x++)
    //            {
    //                for (int y = 0; y < texture.height; y++)
    //                {
    //                    heights[x, y] = rawHeights[i];
    //                    i++;
    //                }
    //            }
    //        }

    //        terrain.terrainData.SetHeights(0, 0, heights);
    //    }
    //}

    //public void DispatchGpu()
    //{
    //    // Points buffer is populated inside shader with pos (xyz) + density (w). Set paramaters
    //    TerrainCompute.SetTexture(0, "Result", this.ComputeTexture);
    //    TerrainCompute.SetInt("Resolution", Resolution);

    //    // Dispatch shader
    //    int groups = Mathf.CeilToInt(Resolution / 8f);
    //    TerrainCompute.Dispatch(0, groups, groups, 1);
    //}
}
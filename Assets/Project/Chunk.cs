using System.Collections.Generic;
using UnityEngine;
using static FastNoiseLite;
using static MeshGeneration;

public class Chunk
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uv;
    public Color[] colors;

    private List<NoiseSetup> noisesSetup;
    private int sizeMesh;
    private float yWater;
    private float sizePolygon;
    private int currentSeed;

    public Chunk(int sizeMesh, float yWater, float sizePolygon, int currentSeed, List<NoiseSetup> noisesSetup)
    {
        this.sizeMesh = sizeMesh;
        this.yWater = yWater;
        this.sizePolygon = sizePolygon;
        this.currentSeed = currentSeed;
        this.noisesSetup = noisesSetup;
    }

    public Chunk CreateChunk(Vector3 pos)
    {
        var vert = new Vector3[(sizeMesh + 1) * (sizeMesh + 1)];
        var tris = new int[sizeMesh * sizeMesh * 6];
        var uvs = new Vector2[vert.Length];
        var clr = new Color[vert.Length];
        var noise = new FastNoiseLite();
        noise.SetFrequency(0.05f);
        var noiseStone = new FastNoiseLite();
        noiseStone.SetFractalOctaves(4);
        noiseStone.SetNoiseType(NoiseType.Cellular);
        noiseStone.SetFrequency(0.4f);

        for (int i = 0, z = 0; z <= sizeMesh; z++)
        {
            for (int x = 0; x <= sizeMesh; x++)
            {
                float height = GetNoise(pos.x + x * sizePolygon, pos.z + z * sizePolygon);
                vert[i] = new Vector3(x * sizePolygon, height, z * sizePolygon);
                if (height < yWater + noise.GetNoise(pos.x + x * sizePolygon, pos.z + z * sizePolygon))
                {
                    clr[i] = Color.blue;
                }
                else
                {
                    if (noiseStone.GetNoise(pos.x + x * sizePolygon, pos.z + z * sizePolygon) > 0.6f)
                    {
                        clr[i] = Color.green;
                    }
                    else
                    {
                        clr[i] = Color.red;
                    }
                }
                uvs[i] = new Vector2((float)x / sizeMesh, (float)z / sizeMesh);
                i++;
            }
        }

        for (int ti = 0, vi = 0, y = 0; y < sizeMesh; y++, vi++)
        {
            for (int x = 0; x < sizeMesh; x++, ti += 6, vi++)
            {
                tris[ti] = vi;
                tris[ti + 3] = tris[ti + 2] = vi + 1;
                tris[ti + 4] = tris[ti + 1] = vi + sizeMesh + 1;
                tris[ti + 5] = vi + sizeMesh + 2;
            }
        }

        vertices = vert;
        triangles = tris;
        uv = uvs;
        colors = clr;
        return this;
    }

    public float GetNoise(float x, float y, List<NoiseSetup> noises = null)
    {
        float result = 0;
        float add = 0;
        var fNoise = new FastNoiseLite();
        foreach (var noise in (noises != null ? noises : noisesSetup))
        {
            fNoise.SetNoiseType(noise.noiseType);
            fNoise.SetRotationType3D(noise.rotationType);
            fNoise.SetDomainWarpType(noise.domainWarpType);
            fNoise.SetFractalType(noise.fractalType);
            fNoise.SetCellularReturnType(noise.cellularReturnType);
            fNoise.SetCellularDistanceFunction(noise.cellularDistanceFunction);
            fNoise.SetFrequency(noise.frequency);
            fNoise.SetFractalGain(noise.gain);
            fNoise.SetFractalOctaves(noise.octave);
            fNoise.SetSeed(noise.seed + currentSeed);
            add = fNoise.GetNoise(x, y) * noise.addHeight;
            if ((noise.yMax != 0 && noise.yMax < add) || (noise.yMin != 0 && noise.yMin > add))
            {
                continue;
            }
            if (noise.addNoises.Count > 0)
            {
                add *= GetNoise(x, y, noise.addNoises);
            }
            switch (noise.mathType)
            {
                case MathType.ADD:
                    result += add;
                    break;
                case MathType.MULTIPLY:
                    result *= add;
                    break;
                case MathType.SUB:
                    result -= add;
                    break;
            }
        };
        return result;
    }
}

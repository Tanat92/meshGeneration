using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static FastNoiseLite;

public class MeshGeneration : MonoBehaviour
{
    public int sizeMesh = 30;
    public int sizeChunks = 6;
    public int globalSeed = 0;
    public List<NoiseSetup> noisesSetup = new List<NoiseSetup>();
    public MeshFilter[] chunks;
    public Material material;

    public enum MathType
    {
        ADD = 0,
        MULTIPLY = 1,
        SUB = 2
    }

    [System.Serializable]
    public struct NoiseSetup
    {
        public RotationType3D rotationType;
        public CellularDistanceFunction cellularDistanceFunction;
        public CellularReturnType cellularReturnType;
        public FractalType fractalType;
        public NoiseType noiseType;
        public DomainWarpType domainWarpType;
        [Range(0.0f, 0.1f)]
        public float frequency;
        [Range(0.0f, 100.0f)]
        public float addHeight;
        [Range(0.0f, 100.0f)]
        public float gain;
        [Range(0, 9)]
        public int octave;
        [Range(0, 99999)]
        public int seed;
        public MathType mathType;
    }

    [ContextMenu("Clear Chunks")]
    public void ClearChunks()
    {
        foreach (var chunk in chunks)
        {
            if (chunk != null)
            {
                DestroyImmediate(chunk.gameObject);
            }
        }
        chunks = null;
    }

    [ContextMenu("Generation")]
    public void Generation() {
        if (globalSeed == 0)
        {
            globalSeed = Random.Range(-99999, 99999);
        }

        if (chunks?.Length > 0 && chunks.Length != sizeChunks * sizeChunks)
        {
            ClearChunks();
        }

        if (chunks?.Length == 0 || chunks == null)
        {
            chunks = new MeshFilter[sizeChunks * sizeChunks];
            int chn = 0;
            for (int z = 0; z < sizeChunks; z++)
            {
                for (int x = 0; x < sizeChunks; x++)
                {
                    GameObject gmj = new GameObject("Chunk");
                    gmj.transform.SetParent(transform, false);
                    gmj.AddComponent<MeshRenderer>().material = material;
                    gmj.transform.position = new Vector3(x * sizeMesh ,0 ,z * sizeMesh);
                    chunks[chn] = gmj.AddComponent<MeshFilter>();
                    chn++;
                }
            }
        }

        var indexChunk = 0;
        for (int zChunk = 0; zChunk < sizeChunks; zChunk++)
        {
            for (int xChunk = 0; xChunk < sizeChunks; xChunk++)
            {
                var currentChunk = chunks[indexChunk];

                var mesh = new Mesh();
                var vertices = new Vector3[(sizeMesh + 1) * (sizeMesh + 1)];
                var triangles = new int[sizeMesh * sizeMesh * 6];
                var uv = new Vector2[vertices.Length];

                for (int i = 0, z = 0; z <= sizeMesh; z++)
                {
                    for (int x = 0; x <= sizeMesh; x++)
                    {
                        Vector3 pos = currentChunk.transform.position;
                        float height = GetNoise(pos.x + x, pos.z + z);
                        vertices[i] = new Vector3(x, height, z);
                        uv[i] = new Vector2((float)x / sizeMesh, (float)z / sizeMesh);
                        i++;
                    }
                }

                for (int ti = 0, vi = 0, y = 0; y < sizeMesh; y++, vi++)
                {
                    for (int x = 0; x < sizeMesh; x++, ti += 6, vi++)
                    {
                        triangles[ti] = vi;
                        triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                        triangles[ti + 4] = triangles[ti + 1] = vi + sizeMesh + 1;
                        triangles[ti + 5] = vi + sizeMesh + 2;
                    }
                }

                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uv;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                currentChunk.sharedMesh = mesh;
                indexChunk++;
            }
        }
    }

    public float GetNoise(float x, float y)
    {
        float result = 0;
        var fNoise = new FastNoiseLite();
        noisesSetup.ForEach(noise =>
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
            fNoise.SetSeed(noise.seed + globalSeed);
            switch (noise.mathType)
            {
                case MathType.ADD:
                    result += fNoise.GetNoise(x, y) * noise.addHeight;
                    break;
                case MathType.MULTIPLY:
                    result *= fNoise.GetNoise(x, y) * noise.addHeight;
                    break;
                case MathType.SUB:
                    result -= fNoise.GetNoise(x, y) * noise.addHeight;
                    break;
            }
        });
        return result;
    }
}

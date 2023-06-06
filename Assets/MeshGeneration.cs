using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static FastNoiseLite;

public class MeshGeneration : MonoBehaviour
{
    public int sizeMesh = 30;
    public float sizePolygon = 1f;
    public int sizeChunks = 6;
    public int globalSeed = 0;
    public float yWater = 0;
	public int maxTrees = 30;
    public GameObject[] prefabTrees;
    public List<NoiseSetup> noisesSetup = new List<NoiseSetup>();
    public MeshFilter[] chunks;
    public Material material;

    private int currentSeed;
    private List<GameObject> trees = new List<GameObject>();

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
        public List<NoiseSetup> addNoises;
        [Range(0.0f, 0.1f)]
        public float frequency;
        [Range(0.0f, 100.0f)]
        public float addHeight;
        [Range(0.0f, 100.0f)]
        public float gain;
        [Range(1, 9)]
        public int octave;
        [Range(0, 99999)]
        public int seed;
        public MathType mathType;
        [Range(-100f, 100f)]
        public float yMax;
        [Range(-100f, 100f)]
        public float yMin;
    }

    [ContextMenu("Clear Chunks")]
    public void ClearChunks()
    {
        if (trees.Any())
        {
            trees.ForEach(t =>
            {
                if (t != null)
                {
                    DestroyImmediate(t);
                }
            });
            trees.Clear();
        }
        if (chunks != null)
        {
            foreach (var chunk in chunks)
            {
                if (chunk != null)
                {
                    DestroyImmediate(chunk.gameObject);
                }
            }
        }
        chunks = null;
    }

    private void TreeGeneration()
    {
        if (trees.Any())
        {
            trees.ForEach(t =>
            {
                if (t != null)
                {
                    DestroyImmediate(t);
                }
            });
            trees.Clear();
        }
        RaycastHit hit;
        while (trees.Count <= maxTrees)
        {
            var chunk = chunks[Random.Range(0, chunks.Length - 1)];
            int rand = Random.Range(0, chunk.mesh.vertices.Length - 1);
            if (chunk.mesh.vertices[rand].y > 3)
            {
                var tree = Instantiate(prefabTrees[Random.Range(0, prefabTrees.Length - 1)], chunk.mesh.vertices[rand] + chunk.transform.position - Vector3.up * 0.3f, Quaternion.identity);
                tree.transform.SetParent(transform, false);
                tree.transform.localScale = Vector3.one * Random.Range(1f, 1.5f);
                if (Physics.Raycast(chunk.mesh.vertices[rand] + chunk.transform.position + Vector3.up * 5, Vector3.down * 10, out hit))
                {
                    tree.transform.localEulerAngles = hit.point.normalized;
                }
                tree.transform.Rotate(Vector3.up, Random.Range(0, 360));
                trees.Add(tree);
            }
        }
    }

    [ContextMenu("Generation")]
    public void Generation() {
        if (globalSeed == 0)
        {
            currentSeed = Random.Range(-99999, 99999);
        }
        else
        {
            currentSeed = globalSeed;
        }
        ClearChunks();

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
                    gmj.AddComponent<MeshCollider>();
                    gmj.AddComponent<MeshRenderer>().material = material;
                    gmj.transform.position = new Vector3(x * sizeMesh * sizePolygon ,0 ,z * sizeMesh * sizePolygon);
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
                var colors = new Color[vertices.Length];
                var noise = new FastNoiseLite();
                noise.SetFrequency(0.05f);

                for (int i = 0, z = 0; z <= sizeMesh; z++)
                {
                    for (int x = 0; x <= sizeMesh; x++)
                    {
                        Vector3 pos = currentChunk.transform.position;
                        float height = GetNoise(pos.x + x * sizePolygon, pos.z + z * sizePolygon);
                        vertices[i] = new Vector3(x * sizePolygon, height, z * sizePolygon);
                        if (height < yWater + noise.GetNoise(pos.x + x * sizePolygon, pos.z + z * sizePolygon))
                        {
                            colors[i] = Color.blue;
                        }
                        else
                        {
                            colors[i] = Color.red;
                        }
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
                mesh.colors = colors.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                currentChunk.GetComponent<MeshCollider>().sharedMesh = mesh;
                currentChunk.sharedMesh = mesh;
                indexChunk++;
            }
        }

        TreeGeneration();
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

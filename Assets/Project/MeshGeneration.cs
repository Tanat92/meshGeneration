using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class MeshGeneration : MonoBehaviour
{
    public int sizeMesh = 30;
    public float sizePolygon = 1f;
    public int sizeChunks = 6;
    public int globalSeed = 0;
    public float yWater = 0;
	public int maxTrees = 30;
	public int maxGrass = 30;
    public GameObject[] prefabTrees;
    public GameObject prefabGrass;
    public List<NoiseSetup> noisesSetup = new List<NoiseSetup>();
    public Dictionary<Vector3, MeshFilter> chunks = new Dictionary<Vector3, MeshFilter>();
    public Material material;
    public LayerMask layerMask;
    public Transform player;

    public bool isGeneration = false;

    private int currentSeed;
    private List<GameObject> grasses = new List<GameObject>();

    private List<Vector3> farChunks = new List<Vector3>();
    private List<Vector3> newChunks = new List<Vector3>();

    public enum MathType
    {
        ADD = 0,
        MULTIPLY = 1,
        SUB = 2
    }

    [ContextMenu("Clear Chunks")]
    public void ClearChunks()
    {
        if (transform.childCount != 0)
        {
            while (transform.childCount != 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
        chunks.Clear();
    }

    private void Start()
    {
        Generation();
    }

    private void Update()
    {
        if (!isGeneration)
        {
            var size = sizePolygon * sizeMesh;
            Vector3 centerChunkPlayer = new Vector3((int)(player.position.x / size) * size, 0, (int)(player.position.z / size) * size);
            Vector3 centerChunk = new Vector3(sizeChunks / 2 * size, 0, sizeChunks / 2 * size);
            int count = 0;
            var lf = new List<Vector3>();
            var ln = new List<Vector3>();
            for (int z = 0; z < sizeChunks; z++)
            {
                for (int x = 0; x < sizeChunks; x++)
                {
                    if (!chunks.ContainsKey(centerChunkPlayer - centerChunk + new Vector3(size * x, 0, size * z)))
                    {
                        ln.Add(centerChunkPlayer - centerChunk + new Vector3(size * x, 0, size * z));
                        count++;
                    }
                    else
                    {
                        lf.Add(centerChunkPlayer - centerChunk + new Vector3(size * x, 0, size * z));
                        count++;
                    }
                }
            }
            if (ln.Count > 0)
            {
                newChunks = ln;
                farChunks = chunks.Where(a => !lf.Contains(a.Key)).Select(a => a.Key).ToList();
                GenerationNewChunk();
            }
        }
    }

    [ContextMenu("Generation")]
    public async void Generation() {
        if (globalSeed == 0)
        {
            currentSeed = Random.Range(-99999, 99999);
        }
        else
        {
            currentSeed = globalSeed;
        }
        ClearChunks();

        if (chunks?.Count == 0 || chunks == null)
        {
            int chn = 0;
            for (int z = 0; z < sizeChunks; z++)
            {
                for (int x = 0; x < sizeChunks; x++)
                {
                    GameObject gmj = new GameObject("Chunk");
                    gmj.transform.SetParent(transform, false);
                    //gmj.layer = layerMask;
                    gmj.AddComponent<MeshCollider>();
                    var rb = gmj.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    gmj.AddComponent<MeshRenderer>().material = material;
                    gmj.transform.position = new Vector3(x * sizeMesh * sizePolygon ,0 ,z * sizeMesh * sizePolygon);
                    chunks[gmj.transform.position] = gmj.AddComponent<MeshFilter>();
                    chn++;
                }
            }
        }
        var indexChunk = 0;
        var chunksList = new List<Chunk>();
        int count = 0;

        using (var resetEvent = new ManualResetEvent(false))
        {
            for (int zChunk = 0; zChunk < sizeChunks; zChunk++)
            {
                for (int xChunk = 0; xChunk < sizeChunks; xChunk++)
                {
                    var pos = new Vector3(xChunk * sizeMesh * sizePolygon, 0, zChunk * sizeMesh * sizePolygon);
                    var chunk = new Chunk(sizeMesh, yWater, sizePolygon, currentSeed, noisesSetup);
                    chunksList.Add(chunk);
                    var index = indexChunk;
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        chunk = chunk.CreateChunk(pos);
                        lock (chunksList)
                        {
                            chunksList[index] = chunk;
                        }
                        if (Interlocked.Increment(ref count) >= sizeChunks * sizeChunks)
                        {
                            resetEvent.Set();
                        }
                    });
                    indexChunk++;
                }
            }
            resetEvent.WaitOne();
        }

        indexChunk = 0;
        for (int zChunk = 0; zChunk < sizeChunks; zChunk++)
        {
            for (int xChunk = 0; xChunk < sizeChunks; xChunk++)
            {
                var pos = new Vector3(xChunk * sizeMesh * sizePolygon, 0, zChunk * sizeMesh * sizePolygon);
                var currentChunk = chunks[pos];
                var chunk = chunksList[indexChunk];
                var mesh = new Mesh();
                mesh.vertices = chunk.vertices;
                mesh.triangles = chunk.triangles;
                mesh.uv = chunk.uv;
                mesh.colors = chunk.colors;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                currentChunk.GetComponent<MeshCollider>().sharedMesh = mesh;
                currentChunk.sharedMesh = mesh;
                indexChunk++;
            }
        }
    }

    private void GenerationNewChunk()
    {
        isGeneration = true;
        var list = new ConcurrentDictionary<MeshFilter, Chunk>();
        var max = 0;

        foreach (var chunkPos in newChunks)
        {
            var farChunkPos = farChunks.First();
            var mf = chunks[farChunkPos];
            mf.gameObject.SetActive(false);
            chunks.Remove(farChunkPos);
            chunks[chunkPos] = mf;
            mf.transform.position = chunkPos;
            farChunks.Remove(farChunkPos);

            Chunk chunk = new Chunk(sizeMesh, yWater, sizePolygon, currentSeed, noisesSetup);

            ThreadPool.QueueUserWorkItem(o =>
            {
                list[mf] = chunk.CreateChunk(chunkPos);
            });
            max++;
        }

        StartCoroutine(Test(list, max));
    }

    IEnumerator Test(ConcurrentDictionary<MeshFilter, Chunk> list, int max)
    {
        var count = 0;
        while (count != max )
        {
            if (list.Count > 0)
            {
                lock (list)
                {
                    var chn = list.First();
                    if (chn.Key == null)
                    {
                        continue;
                    }
                    var pos = chn.Key.transform.position;
                    var currentChunk = chunks[pos];
                    var mesh = new Mesh();
                    mesh.vertices = chn.Value.vertices;
                    mesh.triangles = chn.Value.triangles;
                    mesh.uv = chn.Value.uv;
                    mesh.colors = chn.Value.colors.ToArray();
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                    currentChunk.GetComponent<MeshCollider>().sharedMesh = mesh;
                    currentChunk.sharedMesh = mesh;
                    chn.Key.gameObject.SetActive(true);
                    list.Remove(chn.Key, out _);
                    count++;
                }
            }
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForFixedUpdate();
        isGeneration = false;
    } 

    private void OnDrawGizmos()
    {
        var size = sizePolygon * sizeMesh;
        Vector3 centerChunkPlayer = new Vector3((int)(player.position.x / size) * size, 0, (int)(player.position.z / size) * size);
        Vector3 centerChunk = new Vector3(sizeChunks / 2 * size, 0, sizeChunks / 2 * size);
        Gizmos.color = Color.red;
        for (int z = 0; z < sizeChunks; z++)
        {
            for (int x = 0; x < sizeChunks; x++)
            {
                Gizmos.DrawWireCube(centerChunkPlayer - centerChunk + new Vector3(size * x, 0, size * z) + (Vector3.one * size) / 2, Vector3.one * size);
            }
        }
        Gizmos.color = Color.blue;
        if (chunks != null)
        {
            foreach (var chunk in chunks)
            {
                Gizmos.DrawWireCube(chunk.Key + (Vector3.one * size) / 2, Vector3.one * size + Vector3.up * size * 2);
            }
        }
        Gizmos.color = Color.yellow;
        foreach (var chunk in farChunks)
        {
            Gizmos.DrawWireCube(chunk + (Vector3.one * size) / 2, Vector3.one * size + Vector3.up * size * 2);
        }
        Gizmos.color = Color.magenta;
        foreach (var chunk in newChunks)
        {
            Gizmos.DrawWireCube(chunk + (Vector3.one * size) / 2, Vector3.one * size + Vector3.up * size * 2);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using static FastNoiseLite;

public partial class MeshGeneration
{
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
}

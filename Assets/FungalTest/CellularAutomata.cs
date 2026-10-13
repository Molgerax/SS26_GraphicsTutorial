using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MechTech
{
    public class CellularAutomata : MonoBehaviour
    {
        [SerializeField] private ComputeShader cs;
        [SerializeField] private int resolution = 256;
        [SerializeField] private RenderTexture texture;

        [SerializeField] private Renderer render;
        
        [SerializeField, Range(0, 60)] private float updatesPerSecond = 10;

        [SerializeField] private Vector3 massParameters;
        [SerializeField] private Vector3 attractantParameters;
        [SerializeField] private float physarumAttraction = 1;
        [SerializeField] private float chemoConsumption = 1;
        
        [Header("Noise")]
        [SerializeField] private float noiseScale = 1;

        [SerializeField] private float noiseCutoff = 0.1f;
        
        private float _updateTimer;
        private int _simulationStep;
        private MaterialPropertyBlock _propertyBlock;


        private RenderTexture _readTexture;
        
        private void Awake()
        {
            texture = new RenderTexture(resolution, resolution, GraphicsFormat.R32_UInt, GraphicsFormat.None);
            texture.name = "FungalTexture";
            texture.enableRandomWrite = true;
            texture.useMipMap = false;
            texture.Create();

            _readTexture = new RenderTexture(resolution, resolution, GraphicsFormat.R32_UInt, GraphicsFormat.None);
            _readTexture.name = "FungalReadTexture";
            _readTexture.enableRandomWrite = true;
            _readTexture.useMipMap = false;
            _readTexture.Create();
            
            _propertyBlock = new();

            if (render)
            {
                render.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetTexture(PropertyIDs.TextureWrite, texture);
                _propertyBlock.SetInt(PropertyIDs.Resolution, resolution);
                render.SetPropertyBlock(_propertyBlock);
            }
            
            Initialize();
        }

        private void OnDestroy()
        {
            texture.Release();
            texture = null;
            
            _readTexture.Release();
            _readTexture = null;
        }


        private void Update()
        {
            if (updatesPerSecond == 0)
                return;
            
            float timeStep = 1f / updatesPerSecond;
            _updateTimer += Time.deltaTime;

            int safeGuard = 0;
            
            while (_updateTimer > timeStep && safeGuard < 100)
            {
                _updateTimer -= timeStep;
                safeGuard++;

                Tick();
            }
        }

        private void Initialize()
        {
            int kernelId = cs.FindKernel("Initialize");
            _simulationStep = 0;
            RunComputeKernel(kernelId);
        }
        
        
        private void Tick()
        {
            int kernelId = cs.FindKernel("Tick");
            RunComputeKernel(kernelId);
            _simulationStep++;
        }

        private void RunComputeKernel(int kernelId)
        {
            if (kernelId < 0)
                return;
            
            cs.SetTexture(kernelId, PropertyIDs.TextureWrite, texture);
            cs.SetTexture(kernelId, PropertyIDs.TextureRead, _readTexture);
            
            cs.SetInt(PropertyIDs.Step, _simulationStep);
            cs.SetInt(PropertyIDs.Resolution, resolution);
            cs.SetFloat(PropertyIDs.Time, Time.time);

            cs.SetVector(PropertyIDs.MassParam, massParameters);
            cs.SetVector(PropertyIDs.AttractParam, attractantParameters);
            cs.SetFloat(PropertyIDs.PhysarumAttraction, physarumAttraction);
            cs.SetFloat(PropertyIDs.ChemoConsumption, chemoConsumption);
            
            cs.SetFloat(PropertyIDs.NoiseScale, noiseScale);
            cs.SetFloat(PropertyIDs.NoiseCutoff, noiseCutoff);

            
            cs.DispatchExact(kernelId, resolution, resolution);
            
            CopyResults();
        }

        private void CopyResults()
        {
            int index = cs.FindKernel("Copy");
            cs.SetTexture(index, PropertyIDs.TextureWrite, _readTexture);
            cs.SetTexture(index, PropertyIDs.TextureRead, texture);
            cs.DispatchExact(index, resolution, resolution);
        }


        static class PropertyIDs
        {
            public static readonly int TextureWrite = Shader.PropertyToID("_TextureWrite");
            public static readonly int TextureRead = Shader.PropertyToID("_TextureRead");
            public static readonly int Resolution = Shader.PropertyToID("_Resolution");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int Step = Shader.PropertyToID("_Step");
            
            public static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
            public static readonly int NoiseCutoff = Shader.PropertyToID("_NoiseCutoff");
            
            public static readonly int PhysarumAttraction = Shader.PropertyToID("_PhysarumAttraction");
            public static readonly int ChemoConsumption = Shader.PropertyToID("_ChemoConsumption");
            public static readonly int MassParam = Shader.PropertyToID("_PMP");
            public static readonly int AttractParam = Shader.PropertyToID("_CAP");
        }
    }
}

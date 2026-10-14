using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MechTech
{
    public class SlimeAgents : MonoBehaviour
    {
        [SerializeField] private ComputeShader cs;
        [SerializeField] private int agentCount = 65536;
        [SerializeField] private int resolution = 256;
        [SerializeField] private RenderTexture texture;
        
        [SerializeField] private Renderer render;
        
        [SerializeField] private float decaySpeed;
        [SerializeField] private float agentSpeed;
        [SerializeField] private float diffuseSpeed = 1;
        [SerializeField] private float chemoConsumption = 1;
        
        [Header("Noise")]
        [SerializeField] private float noiseScale = 1;

        [SerializeField] private float noiseCutoff = 0.1f;
        
        private float _updateTimer;
        private int _simulationStep;
        private MaterialPropertyBlock _propertyBlock;


        private RenderTexture _readTexture;
        private GraphicsBuffer _agentBuffer;
        
        private void Awake()
        {
            texture = new RenderTexture(resolution, resolution, GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormat.None);
            texture.name = "FungalTexture";
            texture.enableRandomWrite = true;
            texture.useMipMap = false;
            texture.Create();

            _readTexture = new RenderTexture(resolution, resolution, GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormat.None);
            _readTexture.name = "FungalReadTexture";
            _readTexture.enableRandomWrite = true;
            _readTexture.useMipMap = false;
            _readTexture.Create();


            _agentBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, agentCount, sizeof(float) * 16);
            
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
            
            _agentBuffer.Release();
            _agentBuffer = null;
        }


        private void Update()
        {
            if (!texture || _agentBuffer == null)
                return;
            
            Tick();
        }

        private void Initialize()
        {
            int kernelId = cs.FindKernel("Initialize");
            _simulationStep = 0;
            RunComputeKernel(kernelId, agentCount);
            kernelId = cs.FindKernel("InitTexture");
            RunComputeKernel(kernelId, resolution, resolution);
            
            CopyResults();
        }
        
        
        private void Tick()
        {
            int kernelId = cs.FindKernel("Tick");
            RunComputeKernel(kernelId, agentCount);
            
            kernelId = cs.FindKernel("DrawToTexture");
            RunComputeKernel(kernelId, agentCount);
            
            CopyResults();

            kernelId = cs.FindKernel("TickTexture");
            RunComputeKernel(kernelId, resolution, resolution);
            
            CopyResults();
            
            _simulationStep++;
        }

        private void RunComputeKernel(int kernelId, int countX, int countY = 1)
        {
            if (kernelId < 0)
                return;
            
            cs.SetTexture(kernelId, PropertyIDs.TextureWrite, texture);
            cs.SetTexture(kernelId, PropertyIDs.TextureRead, _readTexture);
            cs.SetBuffer(kernelId, PropertyIDs.AgentBuffer, _agentBuffer);
            
            cs.SetInt(PropertyIDs.AgentCount, agentCount);
            cs.SetInt(PropertyIDs.Step, _simulationStep);
            cs.SetInt(PropertyIDs.Resolution, resolution);
            cs.SetFloat(PropertyIDs.Time, Time.time);
            cs.SetFloat(PropertyIDs.DeltaTime, Time.deltaTime);
            
            cs.SetFloat(PropertyIDs.AgentSpeed, agentSpeed);
            cs.SetFloat(PropertyIDs.DecaySpeed, decaySpeed);
            cs.SetFloat(PropertyIDs.DiffuseSpeed, diffuseSpeed);
            
            cs.SetFloat(PropertyIDs.NoiseScale, noiseScale);
            cs.SetFloat(PropertyIDs.NoiseCutoff, noiseCutoff);
            
            cs.DispatchExact(kernelId, countX, countY);
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
            public static readonly int AgentBuffer = Shader.PropertyToID("_AgentBuffer");
            public static readonly int TextureWrite = Shader.PropertyToID("_TextureWrite");
            public static readonly int TextureRead = Shader.PropertyToID("_TextureRead");
            public static readonly int Resolution = Shader.PropertyToID("_Resolution");
            public static readonly int AgentCount = Shader.PropertyToID("_AgentCount");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            public static readonly int Step = Shader.PropertyToID("_Step");
            
            public static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");
            public static readonly int NoiseCutoff = Shader.PropertyToID("_NoiseCutoff");
            
            public static readonly int DecaySpeed = Shader.PropertyToID("_DecaySpeed");
            public static readonly int AgentSpeed = Shader.PropertyToID("_AgentSpeed");
            public static readonly int DiffuseSpeed = Shader.PropertyToID("_DiffuseSpeed");
            public static readonly int AttractParam = Shader.PropertyToID("_CAP");
        }
    }
}

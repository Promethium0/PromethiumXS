using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.D3DCompiler;
using Device = SharpDX.Direct3D11.Device;

namespace PromethiumXS
{
    // Define a simple vertex structure.
    struct Vertex
    {
        public Vector3 Position;
        public RawColor4 Color;

        public Vertex(Vector3 position, RawColor4 color)
        {
            Position = position;
            Color = color;
        }
    }

    // Constant buffer structure.
    struct ConstantBufferData
    {
        public Matrix WorldViewProjection;
    }

    public class Renderer3D : IDisposable
    {
        private Control _renderControl;
        private DisplayListManager _displayListManager;
        private Memory _memory;

        private Device _device;
        private SwapChain _swapChain;
        private RenderTargetView _renderTargetView;
        private DepthStencilView _depthStencilView;

        // Shader objects
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private SharpDX.Direct3D11.Buffer _constantBuffer;

        // For simplicity use an identity matrix for WVP.
        private Matrix _worldViewProjection = Matrix.Identity;

        public Renderer3D(Control renderControl, DisplayListManager displayListManager, Memory memory)
        {
            _renderControl = renderControl;
            _displayListManager = displayListManager;
            _memory = memory;

            InitializeDeviceResources();
            _renderControl.Paint += OnRender;
            _renderControl.Resize += OnResize;
        }

        // Move GetWorldViewProjection method inside the class.
        private Matrix GetWorldViewProjection()
        {
            // Set up a basic camera behind the model looking at the origin.
            Vector3 eye = new Vector3(0, 0, -5);
            Vector3 target = Vector3.Zero;
            Vector3 up = Vector3.UnitY;
            Matrix view = Matrix.LookAtLH(eye, target, up);

            float aspectRatio = _renderControl.ClientSize.Width / (float)_renderControl.ClientSize.Height;
            Matrix projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, aspectRatio, 0.1f, 100f);

            return view * projection;
        }

        private void InitializeDeviceResources()
        {
            var swapChainDescription = new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(
                    _renderControl.ClientSize.Width,
                    _renderControl.ClientSize.Height,
                    new Rational(60, 1),
                    Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = _renderControl.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                swapChainDescription,
                out _device,
                out _swapChain);

            using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
            {
                _renderTargetView = new RenderTargetView(_device, backBuffer);
            }

            var depthBufferDesc = new Texture2DDescription
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = _renderControl.ClientSize.Width,
                Height = _renderControl.ClientSize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            using (var depthBuffer = new Texture2D(_device, depthBufferDesc))
            {
                _depthStencilView = new DepthStencilView(_device, depthBuffer);
            }

            var context = _device.ImmediateContext;
            context.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetView);
            context.Rasterizer.SetViewport(0, 0, _renderControl.ClientSize.Width, _renderControl.ClientSize.Height, 0.0f, 1.0f);

            // Initialize shaders
            InitializeShaders();
        }

        private void InitializeShaders()
        {
            // Simple HLSL source strings.
            string vertexShaderSource = @"
cbuffer ConstantBuffer : register(b0)
{
    matrix WorldViewProjection;
};

struct VS_INPUT
{
    float3 Pos : POSITION;
    float4 Color : COLOR;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    output.Pos = mul(float4(input.Pos, 1.0f), WorldViewProjection);
    output.Color = input.Color;
    return output;
}
";
            string pixelShaderSource = @"
struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
};

float4 PS(PS_INPUT input) : SV_Target
{
    return input.Color;
}
";
            // Compile vertex shader.
            using (var vertexShaderBytecode = ShaderBytecode.Compile(vertexShaderSource, "VS", "vs_5_0", ShaderFlags.Debug))
            {
                _vertexShader = new VertexShader(_device, vertexShaderBytecode);
                // Define input elements for our Vertex.
                var inputElements = new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
                };
                _inputLayout = new InputLayout(_device, ShaderSignature.GetInputSignature(vertexShaderBytecode), inputElements);
            }

            // Compile pixel shader.
            using (var pixelShaderBytecode = ShaderBytecode.Compile(pixelShaderSource, "PS", "ps_5_0", ShaderFlags.Debug))
            {
                _pixelShader = new PixelShader(_device, pixelShaderBytecode);
            }

            // Create a constant buffer.
            _constantBuffer = new SharpDX.Direct3D11.Buffer(
                _device,
                Utilities.SizeOf<ConstantBufferData>(),
                ResourceUsage.Default,
                BindFlags.ConstantBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0);
        }

        private void OnRender(object sender, EventArgs e)
        {
            var context = _device.ImmediateContext;

            // Clear the render target and depth buffer.
            context.ClearRenderTargetView(_renderTargetView, new RawColor4(0.1f, 0.1f, 0.1f, 1.0f));
            context.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            // Update the WorldViewProjection to a proper value.
            _worldViewProjection = GetWorldViewProjection();

            // Update constant buffer.
            ConstantBufferData cbData = new ConstantBufferData { WorldViewProjection = Matrix.Transpose(_worldViewProjection) };
            context.UpdateSubresource(ref cbData, _constantBuffer);

            // Set shaders and constant buffer.
            context.VertexShader.Set(_vertexShader);
            context.VertexShader.SetConstantBuffer(0, _constantBuffer);
            context.PixelShader.Set(_pixelShader);
            context.InputAssembler.InputLayout = _inputLayout;

            // Render from video memory.
            RenderFromVideoMemory(context);

            // Present the frame.
            _swapChain.Present(1, PresentFlags.None);
        }

        private void RenderFromVideoMemory(DeviceContext context)
        {
            const int headerSize = 16; // Size of the header in bytes.
            byte[] videoMemory = _memory.Domains[MemoryDomain.Video];

            if (videoMemory == null || videoMemory.Length < headerSize)
            {
                Console.WriteLine("[Renderer3D] Video memory does not contain enough data for the header.");
                return;
            }

            byte primitiveType = _memory.Read(MemoryDomain.Video, 0);
            int vertexCount = BitConverter.ToInt32(videoMemory, 1);
            int colorCount = BitConverter.ToInt32(videoMemory, 5);
            int indexCount = BitConverter.ToInt32(videoMemory, 9);

            const int videoMemoryStartAddress = 0;
            int vertexDataStart = videoMemoryStartAddress + headerSize;
            int colorDataStart = vertexDataStart + (vertexCount * 12);
            int indexDataStart = colorDataStart + (colorCount * 4);

            int totalDataSize = indexDataStart + (indexCount * 4);
            if (_memory.Domains[MemoryDomain.Video].Length < totalDataSize)
            {
                Console.WriteLine("[Renderer3D] Video memory does not contain enough data for the specified layout.");
                return;
            }

            var positions = new List<Vector3>();
            for (int i = 0; i < vertexCount; i++)
            {
                int addr = vertexDataStart + (i * 12);
                if (addr + 12 > _memory.Domains[MemoryDomain.Video].Length)
                {
                    Console.WriteLine($"[Renderer3D] Vertex data out of bounds at index {i}. Skipping remaining vertices.");
                    break;
                }
                float x = BitConverter.ToSingle(_memory.Domains[MemoryDomain.Video], addr);
                float y = BitConverter.ToSingle(_memory.Domains[MemoryDomain.Video], addr + 4);
                float z = BitConverter.ToSingle(_memory.Domains[MemoryDomain.Video], addr + 8);
                positions.Add(new Vector3(x, y, z));
            }

            RawColor4 defaultColor = new RawColor4(1, 1, 1, 1);
            var colorList = new List<RawColor4>();
            if (colorCount > 0)
            {
                for (int i = 0; i < colorCount; i++)
                {
                    int addr = colorDataStart + (i * 4);
                    if (addr + 4 > _memory.Domains[MemoryDomain.Video].Length)
                    {
                        Console.WriteLine($"[Renderer3D] Color data out of bounds at index {i}. Skipping remaining colors.");
                        break;
                    }
                    float r = _memory.Domains[MemoryDomain.Video][addr] / 255f;
                    float g = _memory.Domains[MemoryDomain.Video][addr + 1] / 255f;
                    float b = _memory.Domains[MemoryDomain.Video][addr + 2] / 255f;
                    float a = _memory.Domains[MemoryDomain.Video][addr + 3] / 255f;
                    colorList.Add(new RawColor4(r, g, b, a));
                }
            }

            RawColor4 modelColor = colorList.Count > 0 ? colorList[0] : defaultColor;
            Vertex[] vertices = new Vertex[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                vertices[i] = new Vertex(positions[i], modelColor);
            }

            context.InputAssembler.PrimitiveTopology = primitiveType switch
            {
                1 => SharpDX.Direct3D.PrimitiveTopology.TriangleList,
                2 => SharpDX.Direct3D.PrimitiveTopology.TriangleStrip,
                _ => SharpDX.Direct3D.PrimitiveTopology.Undefined
            };

            if (vertices.Length > 0)
            {
                using (var vertexBuffer = SharpDX.Direct3D11.Buffer.Create(
                    _device,
                    BindFlags.VertexBuffer,
                    vertices))
                {
                    context.InputAssembler.SetVertexBuffers(0,
                        new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
                    context.Draw(vertices.Length, 0);
                }
            }
            else
            {
                Console.WriteLine("[Renderer3D] No vertices to draw.");
            }

            Console.WriteLine($"[Renderer3D] vertexCount: {vertexCount}, colorCount: {colorCount}, indexCount: {indexCount}");
            Console.WriteLine($"[Renderer3D] vertexDataStart: {vertexDataStart}, colorDataStart: {colorDataStart}, indexDataStart: {indexDataStart}, totalDataSize: {totalDataSize}");
        }

        private void OnResize(object sender, EventArgs e)
        {
            if (_renderControl.ClientSize.Width == 0 || _renderControl.ClientSize.Height == 0)
                return;

            Utilities.Dispose(ref _renderTargetView);
            Utilities.Dispose(ref _depthStencilView);

            _swapChain.ResizeBuffers(1, _renderControl.ClientSize.Width, _renderControl.ClientSize.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);
            using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
            {
                _renderTargetView = new RenderTargetView(_device, backBuffer);
            }

            var depthBufferDesc = new Texture2DDescription
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = _renderControl.ClientSize.Width,
                Height = _renderControl.ClientSize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            using (var depthBuffer = new Texture2D(_device, depthBufferDesc))
            {
                _depthStencilView = new DepthStencilView(_device, depthBuffer);
            }

            var context = _device.ImmediateContext;
            context.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetView);
            context.Rasterizer.SetViewport(0, 0, _renderControl.ClientSize.Width, _renderControl.ClientSize.Height, 0.0f, 1.0f);
        }

        public void Dispose()
        {
            Utilities.Dispose(ref _constantBuffer);
            Utilities.Dispose(ref _inputLayout);
            Utilities.Dispose(ref _vertexShader);
            Utilities.Dispose(ref _pixelShader);
            Utilities.Dispose(ref _renderTargetView);
            Utilities.Dispose(ref _depthStencilView);
            Utilities.Dispose(ref _swapChain);
            Utilities.Dispose(ref _device);
        }

        private bool ValidateCounts(int vertexCount, int colorCount, int indexCount, byte primitiveType)
        {
            Console.WriteLine($"[ValidateCounts] vertexCount: {vertexCount}, colorCount: {colorCount}, indexCount: {indexCount}, primitiveType: {primitiveType}");
            const int headerSize = 16;
            int vertexDataSize = vertexCount * 12;
            int colorDataSize = colorCount * 4;
            int indexDataSize = indexCount * 4;
            int totalRequiredSize = headerSize + vertexDataSize + colorDataSize + indexDataSize;

            if (_memory.Domains[MemoryDomain.Video].Length < totalRequiredSize)
            {
                Console.WriteLine("[Renderer3D] Insufficient video memory for the specified counts.");
                return false;
            }

            if (primitiveType == 1 && indexCount % 3 != 0)
            {
                Console.WriteLine("[Renderer3D] Index count is not a multiple of 3 for TriangleList.");
                return false;
            }

            if (primitiveType == 2 && indexCount < 3)
            {
                Console.WriteLine("[Renderer3D] Index count is too small for TriangleStrip.");
                return false;
            }

            return true;
        }
    }
}

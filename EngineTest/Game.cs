using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.RawInput;
using D3D11 = SharpDX.Direct3D11;
using SharpDX;
using System.Windows.Forms;

/// <summary>
/// TOOD:
/// ZBuffer
/// Texture coordinates
/// Keyboard handler
/// Mouse Handler
/// </summary>

namespace EngineTest
{
    public class RenderObject
    {
        public Matrix transform;
        public SharpDX.Color color;
    }

    // A virtual keyboard that we keep updated with
    // the key down/ups we get. So game can at any time
    // ask if a key is down.
    class Keyboard
    {
        bool[] keyboard = new bool[256];

        internal bool IsDown(Keys key)
        {
            int id = (int)key;
            if (id < 0 || id >= keyboard.Length)
                return false;
            return keyboard[id];
        }

        internal void SetDown(Keys key, bool down)
        {
            int id = (int)key;
            if (id < 0 || id >= keyboard.Length)
                return;
            keyboard[id] = down;
        }
    }

    class Game : IDisposable
    {
        private RenderForm renderForm;

        private const int width = 1280;
        private const int height = 720;

        // Mouse handling
        private int deltaMouseX, deltaMouseY;

        // Our virtual keyboard
        Keyboard keyboard = new Keyboard();

        // Freecam
        Vector3 freeCamPos = new Vector3(0, 0, -20);
        Vector3 freeCamLookDir;
        float freeCamYaw = 0.0f;
        float freeCamPitch = 0.0f;
        float freeCamSpeed = 0.1f;

        private D3D11.Device d3dDevice;
        private D3D11.DeviceContext d3dDeviceContext;
        private SwapChain swapChain;
        private D3D11.RenderTargetView renderTargetView;

        private struct VertexShaderConstants
        {
            public Matrix modelViewProj;
            public Vector4 color;
        }
        private D3D11.VertexShader vertexShader;
        private D3D11.PixelShader pixelShader;

        private D3D11.InputElement[] inputElements = new D3D11.InputElement[]
        {
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0),
        };
        private ShaderSignature inputSignature;
        private D3D11.InputLayout inputLayout;

        private Viewport viewport;

        private List<RenderObject> renderObjects = new List<RenderObject>();

        private float time;

        private D3D11.Buffer constantBuffer;

        private D3D11.Buffer triangleBuffer;
        private D3D11.Buffer cubeBuffer;
        private struct VertexColor
        {
            public Vector3 position;
            public Color4 color;
        }
        private VertexColor[] triangleVertices = new VertexColor[] {
            new VertexColor { position = new Vector3(-0.5f, 0.5f, 0.0f), color = new Color4(0.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(0.5f, 0.5f, 0.0f), color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(0.0f, -0.5f, 0.0f), color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), },
        };

        private VertexColor[] cubeVertices = new VertexColor[] {
            // Front
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            // Back
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            // Top 
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            // Bottom
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            // Left
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            // Bottom
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
        };

        public Game()
        {
            renderForm = new RenderForm("EngineTest");
            renderForm.ClientSize = new Size(width, height);
            renderForm.AllowUserResizing = false;
            InitializeDeviceResources();
            InitializePrimitives();
            InitializeShaders();
            time = 0.0f;
            Cursor.Hide();
            Console.WriteLine("Game initialized...");

            // Create test objects with random position, scale and rotation. 
            var r = new Random();
            for(int i = 0; i < 100; i++)
            {
                var scaleMat = Matrix.Scaling(r.NextVector3(Vector3.One * 0.2f, Vector3.One * 3.0f));
                var rotMat = Matrix.RotationYawPitchRoll(r.NextFloat(0, 0), r.NextFloat(-.1f, .1f), r.NextFloat(-.1f, .1f));
                var translateMat = Matrix.Translation(r.NextVector3(Vector3.One * (-5.0f), Vector3.One * 5.0f));

                var ro = new RenderObject();
                ro.transform = scaleMat * rotMat * translateMat;
                ro.color = r.NextColor();
                renderObjects.Add(ro);
            }
        }

        private void InitializeShaders()
        {
            using (var vertexShaderCode = ShaderBytecode.CompileFromFile("vertex_shader.hlsl", "main", "vs_4_0", ShaderFlags.Debug))
            {
                inputSignature = ShaderSignature.GetInputSignature(vertexShaderCode);
                vertexShader = new D3D11.VertexShader(d3dDevice, vertexShaderCode);
            }
            using (var pixelShaderCode = ShaderBytecode.CompileFromFile("pixel_shader.hlsl", "main", "ps_4_0", ShaderFlags.Debug))
            {
                pixelShader = new D3D11.PixelShader(d3dDevice, pixelShaderCode);
            }

            constantBuffer = new D3D11.Buffer(d3dDevice, Utilities.SizeOf<VertexShaderConstants>(), D3D11.ResourceUsage.Default, D3D11.BindFlags.ConstantBuffer, D3D11.CpuAccessFlags.None, D3D11.ResourceOptionFlags.None, 0);
            d3dDeviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            d3dDeviceContext.VertexShader.Set(vertexShader);
            d3dDeviceContext.PixelShader.Set(pixelShader);
            d3dDeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            inputLayout = new D3D11.InputLayout(d3dDevice, inputSignature, inputElements);
            d3dDeviceContext.InputAssembler.InputLayout = inputLayout;
        }

        private void InitializeDeviceResources()
        {
            ModeDescription backBufferDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
            SwapChainDescription swapChainDescription = new SwapChainDescription()
            {
                ModeDescription = backBufferDescription,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = renderForm.Handle,
                IsWindowed = true
            };
            D3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, D3D11.DeviceCreationFlags.None, swapChainDescription, out d3dDevice, out swapChain);
            d3dDeviceContext = d3dDevice.ImmediateContext;

            viewport = new Viewport(0, 0, width, height);
            d3dDeviceContext.Rasterizer.SetViewport(viewport);

            using(D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0))
            {
                renderTargetView = new D3D11.RenderTargetView(d3dDevice, backBuffer);
            }

            // Create depth buffer. This is a 'texture' where the actual depth values of
            // each pixel is stored
            D3D11.Texture2DDescription depthStencilDescription = new D3D11.Texture2DDescription
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = swapChain.Description.SampleDescription,
                Usage = D3D11.ResourceUsage.Default,
                BindFlags = D3D11.BindFlags.DepthStencil,
            };
            var depthStencil = new D3D11.Texture2D(d3dDevice, depthStencilDescription);

            // Create stencil state description.
            var depthStencilStateDesc = new D3D11.DepthStencilStateDescription
            {
                IsDepthEnabled = true,
                DepthWriteMask = D3D11.DepthWriteMask.All,
                DepthComparison = D3D11.Comparison.Less,
                IsStencilEnabled = true,
                StencilReadMask = 0xff,
                StencilWriteMask = 0xff,
                FrontFace = new D3D11.DepthStencilOperationDescription
                {
                    Comparison = D3D11.Comparison.Always,
                    PassOperation = D3D11.StencilOperation.Keep,
                    DepthFailOperation = D3D11.StencilOperation.Increment,
                    FailOperation = D3D11.StencilOperation.Keep
                },
                BackFace = new D3D11.DepthStencilOperationDescription
                {
                    Comparison = D3D11.Comparison.Always,
                    PassOperation = D3D11.StencilOperation.Keep,
                    DepthFailOperation = D3D11.StencilOperation.Decrement,
                    FailOperation = D3D11.StencilOperation.Keep
                },
            };
            var depthStencilState = new D3D11.DepthStencilState(d3dDevice, depthStencilStateDesc);

            // Create depth stencil view
            depthStencilView = new D3D11.DepthStencilView(d3dDevice, depthStencil, new D3D11.DepthStencilViewDescription
            {
                Dimension = D3D11.DepthStencilViewDimension.Texture2D,
                Format = Format.D32_Float_S8X24_UInt,
            });

            d3dDeviceContext.OutputMerger.DepthStencilState = depthStencilState;
            d3dDeviceContext.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);
        }

        private void InitializePrimitives()
        {
            triangleBuffer = D3D11.Buffer.Create(d3dDevice, D3D11.BindFlags.VertexBuffer, triangleVertices);
            cubeBuffer = D3D11.Buffer.Create(d3dDevice, D3D11.BindFlags.VertexBuffer, cubeVertices);
        }

        public void Run()
        {
            // Set up keyboard input
            renderForm.KeyDown += HandleKeyDown;
            renderForm.KeyUp += HandleKeyUp;
            renderForm.MouseMove += HandleMouseMove;

            RenderLoop.Run(renderForm, RenderCallback);
        }

        // We keep the mouse pointer forced at the center of
        // the window all the time. This avoids us loosing
        // focus. First frame is skipped as the cursor could
        // be anywhere. After first frame we can calculate
        // movement since last frame
        private bool firstMouseMove = true;
        private D3D11.DepthStencilView depthStencilView;

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            var cent = new System.Drawing.Point(renderForm.Width / 2, renderForm.Height / 2);
            if(!firstMouseMove)
            {
                deltaMouseX -= cent.X - e.X;
                deltaMouseY -= cent.Y - e.Y;
            }
            firstMouseMove = false;
            Cursor.Position = renderForm.PointToScreen(cent);
        }

        private void HandleMouseMoveRaw(object sender, MouseInputEventArgs e)
        {
            deltaMouseX += e.X;
            deltaMouseY += e.Y;
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            // Hardcoded escape key!
            if (e.KeyCode == Keys.Escape)
                renderForm.Close();

            keyboard.SetDown(e.KeyCode, true);
        }

        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            keyboard.SetDown(e.KeyCode, false);
        }

        const float TORAD = (float)Math.PI / 180.0f;

        // Tick the game world!
        private void Tick()
        {
            // Update freecam yaw/pitch
            freeCamPitch -= deltaMouseY * 0.001f;
            freeCamPitch = MathUtil.Clamp(freeCamPitch, -80.0f * TORAD, 80.0f * TORAD);
            freeCamYaw += deltaMouseX * 0.001f;
            if (freeCamYaw > 2.0f * MathUtil.Pi) freeCamYaw -= 2.0f * MathUtil.Pi;
            if (freeCamYaw < 0.0f) freeCamYaw += 2.0f * MathUtil.Pi;

            // Create forward vector from yaw/pitch
            Vector3 yawDir = new Vector3((float)Math.Sin(freeCamYaw), 0, (float)Math.Cos(freeCamYaw));
            freeCamLookDir = (float)Math.Cos(freeCamPitch) * yawDir + (float)Math.Sin(freeCamPitch) * Vector3.Up;

            // Calculate the vector pointing to the right when 
            // looking out the camera
            Vector3 right = Vector3.Cross(freeCamLookDir, Vector3.Up);

            // Apply movement relative to camera
            if (keyboard.IsDown(Keys.W))
                freeCamPos += freeCamSpeed * freeCamLookDir;
            if (keyboard.IsDown(Keys.S))
                freeCamPos -= freeCamSpeed * freeCamLookDir;
            if (keyboard.IsDown(Keys.A))
                freeCamPos += freeCamSpeed * right;
            if (keyboard.IsDown(Keys.D))
                freeCamPos -= freeCamSpeed * right;
        }

        private void Draw()
        {
            d3dDeviceContext.ClearRenderTargetView(renderTargetView, new SharpDX.Color(32, 32, 64));
            d3dDeviceContext.ClearDepthStencilView(depthStencilView, D3D11.DepthStencilClearFlags.Depth | D3D11.DepthStencilClearFlags.Stencil, 1.0f, 0);

            // Camera
            var view = Matrix.LookAtLH(freeCamPos, freeCamPos + freeCamLookDir, new Vector3(0, 1, 0));
            var proj = Matrix.PerspectiveFovLH(45.0f * TORAD, (float)width / height, 0.1f, 100.0f);

            var viewProj = Matrix.Multiply(view, proj);

            var binding = new D3D11.VertexBufferBinding(cubeBuffer, Utilities.SizeOf<VertexColor>(), 0);
            d3dDeviceContext.InputAssembler.SetVertexBuffers(0, binding);

            VertexShaderConstants c;
            for(int i = 0; i < renderObjects.Count; i++)
            {
                var o = renderObjects[i];
                c.modelViewProj = o.transform * viewProj;
                c.modelViewProj.Transpose();
                c.color = o.color.ToVector4();

                d3dDeviceContext.UpdateSubresource(ref c, constantBuffer);
                d3dDeviceContext.Draw(cubeVertices.Count(), 0);
            }

            swapChain.Present(1, PresentFlags.None);
        }

        private void RenderCallback()
        {
            // Advance time
            time += 1.0f / 60.0f;

            Tick();
            Draw();

            // clear input
            deltaMouseX = 0;
            deltaMouseY = 0;
        }

        public void Dispose()
        {
            inputSignature.Dispose();
            inputLayout.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();
            triangleBuffer.Dispose();
            cubeBuffer.Dispose();
            renderTargetView.Dispose();
            swapChain.Dispose();
            d3dDevice.Dispose();
            d3dDeviceContext.Dispose();
            renderForm.Dispose();
        }
    }
}

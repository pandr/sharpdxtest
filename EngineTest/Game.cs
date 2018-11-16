﻿using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
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
    class Game : IDisposable
    {
        private RenderForm renderForm;

        private const int width = 1280;
        private const int height = 720;

        private D3D11.Device d3dDevice;
        private D3D11.DeviceContext d3dDeviceContext;
        private SwapChain swapChain;
        private D3D11.RenderTargetView renderTargetView;

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
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 0.0f, 0.0f, 1.0f), },
            // Back
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(0.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(0.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(0.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 1.0f, 0.0f, 1.0f), },
            // Top 
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(0.0f, 0.0f, 1.0f, 1.0f), },
            // Bottom
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 0.0f, 1.0f), },
            // Left
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f, -1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f,  1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            new VertexColor { position = new Vector3(-1.0f,  1.0f, -1.0f), color = new Color4(1.0f, 1.0f, 1.0f, 1.0f), },
            // Bottom
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(0.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f,  1.0f), color = new Color4(0.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f, -1.0f, -1.0f), color = new Color4(0.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f, -1.0f), color = new Color4(0.0f, 0.0f, 0.0f, 1.0f), },
            new VertexColor { position = new Vector3( 1.0f,  1.0f,  1.0f), color = new Color4(0.0f, 0.0f, 0.0f, 1.0f), },
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
            Console.WriteLine("Game initialized...");
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

            constantBuffer = new D3D11.Buffer(d3dDevice, Utilities.SizeOf<Matrix>(), D3D11.ResourceUsage.Default, D3D11.BindFlags.ConstantBuffer, D3D11.CpuAccessFlags.None, D3D11.ResourceOptionFlags.None, 0);
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

            RenderLoop.Run(renderForm, RenderCallback);
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                renderForm.Close();
        }

        private void HandleKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                renderForm.Close();
        }

        const float TORAD = (float)Math.PI / 180.0f;
        private void Draw()
        {
            d3dDeviceContext.OutputMerger.SetRenderTargets(renderTargetView);
            d3dDeviceContext.ClearRenderTargetView(renderTargetView, new SharpDX.Color(32, (int)(100+100.0*Math.Sin(time)), 200));

            // Matrices
            var view = Matrix.LookAtLH(new Vector3(0, 0, -10), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            var proj = Matrix.PerspectiveFovLH(45.0f * TORAD, (float)width / height, 0.1f, 100.0f);

            var viewProj = Matrix.Multiply(view, proj);

            var worldViewProj = /*Matrix.RotationX(time) * Matrix.RotationY(time * 1.2f) * Matrix.RotationZ(time * 1.6f) **/ viewProj;
            worldViewProj.Transpose();

            d3dDeviceContext.UpdateSubresource(ref worldViewProj, constantBuffer);

            var binding = new D3D11.VertexBufferBinding(cubeBuffer, Utilities.SizeOf<VertexColor>(), 0);
            d3dDeviceContext.InputAssembler.SetVertexBuffers(0, binding);
            d3dDeviceContext.Draw(cubeVertices.Count(), 0);

            binding = new D3D11.VertexBufferBinding(triangleBuffer, Utilities.SizeOf<VertexColor>(), 0);
            d3dDeviceContext.InputAssembler.SetVertexBuffers(0, binding);
            d3dDeviceContext.Draw(triangleVertices.Count(), 0);

            swapChain.Present(1, PresentFlags.None);
        }

        private void RenderCallback()
        {
            time += 1.0f / 60.0f;
            Draw();
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

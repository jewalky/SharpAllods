using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace SharpAllods.Rendering
{
    [StructLayout(LayoutKind.Explicit, Pack=1)]
    struct MeshVertex
    {
        [FieldOffset(0)]
        public float u;
        [FieldOffset(sizeof(float))]
        public float v;
        [FieldOffset(sizeof(float) * 2)]
        public byte r;
        [FieldOffset(sizeof(float) * 2 + sizeof(byte))]
        public byte g;
        [FieldOffset(sizeof(float) * 2 + sizeof(byte) * 2)]
        public byte b;
        [FieldOffset(sizeof(float) * 2 + sizeof(byte) * 3)]
        public byte a;
        [FieldOffset(sizeof(float) * 2 + sizeof(byte) * 4)]
        public float x;
        [FieldOffset(sizeof(float) * 3 + sizeof(byte) * 4)]
        public float y;
        [FieldOffset(sizeof(float) * 4 + sizeof(byte) * 4)]
        public float z;
    }

    class Mesh : IDisposable
    {
        private MeshVertex[] Vertices = null;
        private int VerticesGL = 0;
        private int ContextVersion = 0;
        private bool VerticesUpdated = false;
        private PrimitiveType BM = PrimitiveType.Points;

        public Mesh Clone()
        {
            Mesh nmesh = new Mesh(BM);
            
            if (Vertices != null)
            {
                for (int i = 0; i < Vertices.Length; i++)
                    nmesh.SetVertex(i, Vertices[i].x, Vertices[i].y, Vertices[i].z, Vertices[i].u, Vertices[i].v, Vertices[i].r, Vertices[i].g, Vertices[i].b, Vertices[i].a);
            }

            return nmesh;
        }

        public int VertexCount
        {
            get
            {
                return (Vertices != null) ? Vertices.Length : 0;
            }
        }

        public Mesh(PrimitiveType bm)
        {
            BM = bm;
        }

        public void Reset()
        {
            Vertices = null;
            VerticesUpdated = true;
        }

        public void SetVertex(int n, float x, float y, float z, float u, float v, byte r, byte g, byte b, byte a)
        {
            if (Vertices == null)
            {
                Vertices = new MeshVertex[n + 1];
            }
            else if (Vertices.Length <= n)
            {
                MeshVertex[] NewVertices = new MeshVertex[n + 1];
                for (int i = 0; i < Vertices.Length; i++)
                    NewVertices[i] = Vertices[i];
                Vertices = NewVertices;
            }

            if (Vertices[n].x != x ||
                Vertices[n].y != y ||
                Vertices[n].z != z ||
                Vertices[n].u != u ||
                Vertices[n].v != v ||
                Vertices[n].r != r ||
                Vertices[n].g != g ||
                Vertices[n].b != b ||
                Vertices[n].a != a)
            {
                Vertices[n].x = x;
                Vertices[n].y = y;
                Vertices[n].z = z;
                Vertices[n].u = u;
                Vertices[n].v = v;
                Vertices[n].r = r;
                Vertices[n].g = g;
                Vertices[n].b = b;
                Vertices[n].a = a;

                VerticesUpdated = true;
            }
        }

        public void SetVertexColor(int n, byte r, byte g, byte b, byte a)
        {
            if (Vertices == null)
                return;
            if (n < Vertices.Length)
                SetVertex(n, Vertices[n].x, Vertices[n].y, Vertices[n].z, Vertices[n].u, Vertices[n].v, r, g, b, a);
            else SetVertex(n, 0f, 0f, 0f, 0f, 0f, r, g, b, a);
        }

        private static int MeshVertexSize = sizeof(float) * 5 + sizeof(byte) * 4;
        private static int MeshVertexUVOffset = 0;
        private static int MeshVertexColorOffset = sizeof(float) * 2;
        private static int MeshVertexXYZOffset = sizeof(float) * 2 + sizeof(byte) * 4;
        public void Render(int n, string shader)
        {
            if (Vertices == null)
                return;

            if (shader != null)
                Shaders.Get(shader).Activate();

            if (ContextVersion != AllodsWindow.ContextVersion)
            {
                // normally there isnt anything to dispose when the context changes, so we don't need to destroy the old one like in Dispose()
                // generate vbo
                VerticesGL = GL.GenBuffer();
                ContextVersion = AllodsWindow.ContextVersion;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, VerticesGL);
            if (VerticesUpdated && Vertices != null)
            {
                unsafe
                {
                    fixed (MeshVertex* localVertices = Vertices)
                    {
                        GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(MeshVertex) * Vertices.Length), (IntPtr)localVertices, BufferUsageHint.DynamicDraw);
                    }
                }

                VerticesUpdated = false;
            }

            GL.TexCoordPointer(2, TexCoordPointerType.Float, MeshVertexSize, MeshVertexUVOffset);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, MeshVertexSize, MeshVertexColorOffset);
            GL.VertexPointer(3, VertexPointerType.Float, MeshVertexSize, MeshVertexXYZOffset);
            GL.DrawArrays(BM, 0, n);
        }

        public void Render(string shader)
        {
            if (Vertices == null)
                return;
            Render(Vertices.Length, shader);
        }

        public void Dispose()
        {
            if (ContextVersion == AllodsWindow.ContextVersion)
            {
                GL.DeleteBuffer(VerticesGL);
                VerticesGL = 0;
                ContextVersion = 0;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpAllods.Shared;
using OpenTK.Graphics.OpenGL;

namespace SharpAllods.Rendering
{
    class Shaders
    {
        private static Dictionary<string, Shader> GlobalShaders = new Dictionary<string, Shader>();

        public static Shader Get(string shader)
        {
            shader = shader.ToLower();
            if (GlobalShaders.ContainsKey(shader))
                return GlobalShaders[shader];
            // try to load
            Shader newShader = new Shader("shaders/" + shader + ".ash");
            GlobalShaders[shader] = newShader;
            return newShader;
        }
    }

    class Shader : IDisposable
    {
        private string FileName;
        private string CodeFragment = "";
        private string CodeVertex = "";

        private int ShaderGL = 0;
        private int ContextVersion = 0;

        public Shader(string filename)
        {
            FileName = filename;

            MemoryStream ms = ResourceManager.OpenRead(filename);
            if (ms == null)
            {
                Core.Abort("Couldn't load \"{0}\"", filename);
                return;
            }

            string code_vertex = "";
            string code_fragment = "";
            bool in_vertex = false;
            bool in_fragment = false;

            List<string> includes = new List<string>();

            List<string> lines = new List<string>();
            StreamReader sr = new StreamReader(ms);
            while (!sr.EndOfStream)
                lines.Add(sr.ReadLine());
            sr.Close();

            string last_file = filename;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("#shader ") || line.StartsWith("#include ") || line.StartsWith("#returnto "))
                {
                    string[] linep = line.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (linep.Length < 2)
                        continue;

                    if (linep[0] == "#shader")
                    {
                        string shader_type = linep[1].ToLower();
                        if (shader_type == "vertex")
                        {
                            in_vertex = true;
                            in_fragment = false;
                        }
                        else if (shader_type == "fragment")
                        {
                            in_vertex = false;
                            in_fragment = true;
                        }
                        else
                        {
                            Core.Abort("Unknown shader type \"{0}\" in \"{1}\"", shader_type, last_file);
                            return;
                        }

                        continue;
                    }
                    else if (linep[0] == "#returnto")
                    {
                        string ret_filename = linep[1];
                        last_file = ret_filename;
                        continue;
                    }
                    else if (linep[0] == "#include")
                    {
                        string inc_filename = linep[1];

                        MemoryStream msi = ResourceManager.OpenRead(inc_filename);
                        if (msi == null)
                        {
                            Core.Abort("Couldn't load \"{0}\" (included from \"{1}\")", inc_filename, last_file);
                            return;
                        }

                        int inumlines = i+1;
                        sr = new StreamReader(msi);
                        while (!sr.EndOfStream)
                            lines.Insert(inumlines++, sr.ReadLine());
                        sr.Close();
                        lines.Insert(inumlines++, "#returnto " + last_file);
                        last_file = inc_filename;
                        sr.Close();
                        continue;
                    }
                }

                if (in_vertex)
                    code_vertex += line + "\n";
                if (in_fragment)
                    code_fragment += line + "\n";
            }

            if (code_fragment.Length <= 0)
            {
                Core.Abort("No fragment shader found in \"{0}\"", filename);
                return;
            }

            if (code_vertex.Length <= 0)
            {
                Core.Abort("No vertex shader found in \"{0}\"", filename);
                return;
            }

            CodeVertex = code_vertex;
            CodeFragment = code_fragment;
        }

        private void Compile()
        {
            if (ContextVersion != AllodsWindow.ContextVersion)
            {
                int shader_vx = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(shader_vx, CodeVertex);
                GL.CompileShader(shader_vx);
                // check if it compiled
                int shader_vx_success = 0;
                GL.GetShader(shader_vx, ShaderParameter.CompileStatus, out shader_vx_success);
                if (shader_vx_success == 0)
                {
                    string log = GL.GetShaderInfoLog(shader_vx);
                    GL.DeleteShader(shader_vx);
                    Console.WriteLine(" ! Compilation failed:\n{0}\n", log);
                    Core.Abort("Failed to compile vertex shader from \"{0}\"", FileName);
                    return;
                }

                int shader_fr = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(shader_fr, CodeFragment);
                GL.CompileShader(shader_fr);
                // check if it compiled
                int shader_fr_success = 0;
                GL.GetShader(shader_fr, ShaderParameter.CompileStatus, out shader_fr_success);
                if (shader_fr_success == 0)
                {
                    string log = GL.GetShaderInfoLog(shader_fr);
                    GL.DeleteShader(shader_fr);
                    GL.DeleteShader(shader_vx);
                    Console.WriteLine(" ! Compilation failed:\n{0}\n", log);
                    Core.Abort("Failed to compile fragment shader from \"{0}\"", FileName);
                    return;
                }

                ShaderGL = GL.CreateProgram();
                GL.AttachShader(ShaderGL, shader_vx);
                GL.AttachShader(ShaderGL, shader_fr);

                GL.LinkProgram(ShaderGL);

                int program_success = 0;
                GL.GetProgram(ShaderGL, GetProgramParameterName.LinkStatus, out program_success);
                if (program_success == 0)
                {
                    string log = GL.GetProgramInfoLog(ShaderGL);
                    GL.DeleteProgram(ShaderGL);
                    GL.DeleteShader(shader_fr);
                    GL.DeleteShader(shader_vx);
                    ShaderGL = 0;
                    Console.WriteLine(" ! Linking failed:\n{0}\n", log);
                    Core.Abort("Failed to link shader program from \"{0}\"", FileName);
                    return;
                }

                GL.DetachShader(ShaderGL, shader_fr);
                GL.DeleteShader(shader_fr);
                GL.DetachShader(ShaderGL, shader_vx);
                GL.DeleteShader(shader_vx);

                ContextVersion = AllodsWindow.ContextVersion;
            }
        }

        public void Activate()
        {
            Compile();

            GL.UseProgram(ShaderGL);
        }

        public bool SetUniform(string name, params float[] values)
        {
            Compile();
            
            GL.UseProgram(ShaderGL);
            
            int loc = GL.GetUniformLocation(ShaderGL, name);
            if (loc < 0) return false;

            if (values.Length == 1)
                GL.Uniform1(loc, values[0]);
            else if (values.Length == 2)
                GL.Uniform2(loc, values[0], values[1]);
            else if (values.Length == 3)
                GL.Uniform3(loc, values[0], values[1], values[2]);
            else if (values.Length == 4)
                GL.Uniform4(loc, values[0], values[1], values[2], values[3]);
            else return false;

            return true;
        }

        public bool SetUniform(string name, params int[] values)
        {
            Compile();

            GL.UseProgram(ShaderGL);

            int loc = GL.GetUniformLocation(ShaderGL, name);
            if (loc < 0) return false;

            if (values.Length == 1)
                GL.Uniform1(loc, values[0]);
            else if (values.Length == 2)
                GL.Uniform2(loc, values[0], values[1]);
            else if (values.Length == 3)
                GL.Uniform3(loc, values[0], values[1], values[2]);
            else if (values.Length == 4)
                GL.Uniform4(loc, values[0], values[1], values[2], values[3]);
            else return false;

            return true;
        }

        public void Deactivate()
        {
            if (ContextVersion == AllodsWindow.ContextVersion)
            {
                GL.UseProgram(0);
            }
        }

        public void Dispose()
        {
            if (ContextVersion == AllodsWindow.ContextVersion)
            {
                GL.DeleteProgram(ShaderGL);
                ShaderGL = 0;
                ContextVersion = 0;
            }
        }
    }
}

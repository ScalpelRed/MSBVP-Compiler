using System.Runtime.ExceptionServices;
using System.Text;

namespace MSBVPv2.Compiler
{
    public class ShaderVideoFileWriter
    {
        private readonly StreamWriter Writer;
        private bool HasDataVectors = false;
        private bool Finalized = false;

        public string Name = "None";
        public float Fps = 0f;
        public int Width = 0;
        public int Height = 0;
        private int FrameCount = 0;
        private int FrameInd = 0;
        private readonly List<int> FrameInds = [];

        private const int VectorLength = 4;
        private readonly int[] VectorAccum = new int[VectorLength];
        private int VectorHas = 0;

        public ShaderVideoFileWriter(Stream output)
        {
            Writer = new StreamWriter(output);
            Writer.Write("const ivec4[] frameData=ivec4[](");
        }

        public void PushCompressedFrame(int[] frame)
        {
            lock (Writer)
            {
                ThrowIfFinalized();
                if (frame.Length != 0)
                { 
                    FrameInds.Add(FrameInd);
                    FrameInd += frame.Length;
                    FrameCount++;

                    StringBuilder sb = new();
                    void appendIVec4(int x, int y, int z, int w)
                    {
                        if (HasDataVectors) sb.Append(',');
                        sb.Append($"ivec4({x},{y},{z},{w})");
                        HasDataVectors = true;
                    }

                    // Finishing the vector by accum
                    //                 |lim|
                    // VectorAccum: 0 1 X X
                    //       Frame:     0 1 2 3 4 5 6 7 8 9

                    int lim = VectorLength - VectorHas;
                    if (frame.Length < lim) lim = frame.Length;
                    int i = 0;
                    for (; i < lim; i++) VectorAccum[VectorHas + i] = frame[i];
                    VectorHas += lim;
                    if (VectorHas >= VectorLength)
                    {
                        appendIVec4(VectorAccum[0], VectorAccum[1], VectorAccum[2], VectorAccum[3]);
                        VectorHas -= VectorLength;
                    }
                    else return; // if vector is not full, there's definitely no more elements in frame array

                    // Writing all next full groups of 4 as vectors
                    lim = frame.Length - VectorLength + 1;
                    for (; i < lim; i += VectorLength) appendIVec4(frame[i], frame[i + 1], frame[i + 2], frame[i + 3]);

                    // Adding what remains into vector accum (it remains unfinished)
                    lim = frame.Length - i;
                    for (; VectorHas < lim; VectorHas++) VectorAccum[VectorHas] = frame[i + VectorHas];

                    if (sb.Length > 1) // if it's not empty and it's not just comma (at least one vector)
                    {
                        Writer.Write(sb.ToString());
                    }
                }
            }
        }

        public void FinalizeFile()
        {
            lock (Writer)
            {
                ThrowIfFinalized();
                StringBuilder sb = new();

                for (int i = VectorHas; i < VectorLength; i++) VectorAccum[i] = 0; // filling unused components with zeroes, not necessary since they won't be read anyway
                if (HasDataVectors) sb.Append(',');
                sb.Append($"ivec4({VectorAccum[0]},{VectorAccum[1]},{VectorAccum[2]},{VectorAccum[3]})"); // if there was no frame data - we'll end up with one zero vector
                sb.AppendLine(");");

                sb.Append("const int[] frameInds=int[](");
                if (FrameInds.Count == 0) sb.Append('0');
                else sb.AppendJoin(',', FrameInds);
                sb.AppendLine(");");

                sb.AppendLine($"#define name {Name}");
                sb.AppendLine($"#define fps {Fps}");
                sb.AppendLine($"#define frameCount {FrameCount}");
                sb.AppendLine($"#define width {Width}");
                sb.AppendLine($"#define height {Height}");
                Writer.Write(sb.ToString());
                Writer.Close();
                Finalized = true;
            }
        }

        public int GetFrameCount() => FrameCount;

        public int GetDataArrayLength() => FrameInd;

        public int GetIndexArrayLength() => FrameInds.Count;

        public void ThrowIfFinalized()
        {
            if (Finalized) throw new InvalidOperationException("Shader file is already finalized.");
        }
    }
}

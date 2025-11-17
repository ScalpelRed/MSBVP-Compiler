using System.Text;

namespace MSBVPv2.Compiler
{
    public class ShaderVideoFileWriter
    {
        private readonly StreamWriter Writer;
        private bool NotFirstFrame = false;
        private bool Finalized = false;

        public string Name = "None";
        public float Fps = 0f;
        public int Width = 0;
        public int Height = 0;
        private int FrameCount = 0;
        private int FrameInd = 0;
        private readonly List<int> FrameInds = [];

        public ShaderVideoFileWriter(Stream output)
        {
            Writer = new StreamWriter(output);
            Writer.Write("const int[] frameData=int[](");
        }

        public void PushCompressedFrame(int[] frame)
        {
            lock (Writer)
            {
                if (Finalized) throw new InvalidOperationException("Shader file is already finalized.");
                if (frame.Length != 0)
                {
                    StringBuilder sb = new();
                    if (NotFirstFrame) sb.Append(',');
                    else NotFirstFrame = true;
                    sb.AppendJoin(',', frame);
                    Writer.Write(sb.ToString());
                    FrameInds.Add(FrameInd);
                    FrameInd += frame.Length;
                    FrameCount++;
                }
            }
        }

        public void FinalizeFile()
        {
            lock (Writer) {
                StringBuilder sb = new();

                if (!NotFirstFrame) sb.Append('0');
                sb.AppendLine(");");

                sb.Append("const int[] frameInds=int[](");
                
                if (FrameInds.Count == 0) sb.Append('0');
                sb.AppendJoin(',', FrameInds);
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
    }
}

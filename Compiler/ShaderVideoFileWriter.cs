namespace MSBVPv2.Compiler
{
    public class ShaderVideoFileWriter
    {
        private readonly StreamWriter Writer;
        private bool IsFirstFrame = true;
        private bool Finalized;

        public string Name = "None";
        public float Fps = 0f;
        public int Width = 0;
        public int Height = 0;
        public int FrameCount { get; private set; } = 0;
        public int FrameInd { get; private set; } = 0;
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
                    string s = string.Join(',', frame);
                    if (IsFirstFrame)
                    {
                        Writer.Write(s);
                        IsFirstFrame = false;
                    }
                    else
                    {
                        Writer.Write(',');
                        Writer.Write(s);
                    }
                    FrameInds.Add(FrameInd);
                    FrameInd += frame.Length;
                    FrameCount++;
                }
            }
        }

        public void FinalizeFile()
        {
            lock (Writer) {
                Writer.WriteLine(");");

                Writer.Write("const int[] frameInds=int[](");
                Writer.Write(string.Join(',', FrameInds));
                Writer.WriteLine(");");

                Writer.WriteLine($"#define name {Name}");
                Writer.WriteLine($"#define fps {Fps}");
                Writer.WriteLine($"#define frameCount {FrameCount}");
                Writer.WriteLine($"#define width {Width}");
                Writer.WriteLine($"#define height {Height}");

                Finalized = true;
                Writer.Close();
            }
        }
    }
}

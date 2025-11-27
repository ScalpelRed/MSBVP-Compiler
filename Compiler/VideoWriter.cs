namespace MSBVPv2.Compiler
{
    public class VideoWriter
    {
        private readonly string OutDirPath;
        private FileStream DataFile = null!;
        private int DataFileIndex = 0;
        private bool Finished = false;
        private readonly object Lock = new();

        public VideoWriter(string outDir)
        {
            OutDirPath = outDir;
            CreateDataFile();
        }

        public void PushBytes(byte[] data, int count)
        {
            lock (Lock)
            {
                ThrowIfFinished();
                DataFile.Write(data, 0, count);
            }
        }

        private void CreateDataFile()
        {
            DataFile = new FileStream(Path.Combine(OutDirPath, $"data{DataFileIndex}.dat"), FileMode.Create, FileAccess.Write);
            DataFileIndex++;
        }

        public void Finish(string name, int frameWidth, int frameHeight, int texWidth, int texHeight)
        {
            lock (Lock)
            {
                ThrowIfFinished();

                // writing video.glsl
                StreamWriter file = new(Path.Combine(OutDirPath, $"video.glsl"), false);
                file.WriteLine($"#define name {name}");
                file.WriteLine($"#define fwidth {frameWidth}");
                file.WriteLine($"#define fheight {frameHeight}");
                file.Close();

                // writing shaders.properties
                file = new(Path.Combine(OutDirPath, $"shaders.properties"), false);
                for (int i = 0; i < DataFileIndex; i++)
                {
                    file.WriteLine($"texture.gbuffers.data{i} = data{i}.dat TEXTURE_2D RGBA {texWidth} {texHeight} RGBA UNSIGNED_BYTE");
                }
                file.Close();

                Finished = true;
            }
        }

        private void ThrowIfFinished()
        {
            if (Finished) throw new InvalidOperationException("Video writer is already finalized");
        }
    }
}

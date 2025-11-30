using System.Text;

namespace MSBVPv2.Compiler
{
    public class VideoWriter
    {
        private readonly int MaxTexSize;
        private readonly long MaxTexBytes;
        private readonly int MaxTexCount;
        private readonly string OutDirPath;

        private long CurrentTexBytes;
        private FileStream DataFile = null!;
        private int DataFileIndex = 0;
        private bool Finished = false;

        public VideoWriter(int maxTexSize, int maxTexCount, string outDir)
        {
            if (maxTexSize < 0) throw new ArgumentException("Maximum texture size should be above zero", nameof(maxTexSize));
            MaxTexSize = maxTexSize;
            MaxTexBytes = ((long)MaxTexSize * MaxTexSize) << 2;
            MaxTexCount = maxTexCount;
            OutDirPath = outDir;
            CreateDataFile();
        }

        /// <summary>
        /// Writes #count bytes into texture data file (and creates more files if needed)
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="count">Count of bytes to write</param>
        /// <returns>Count of actual bytes written</returns>
        public int PushBytes(byte[] data, int count)
        {
            ThrowIfFinished();
            int srcIndex = 0;
            while (srcIndex < count)
            {
                if (CurrentTexBytes >= MaxTexBytes)
                {
                    CurrentTexBytes -= MaxTexBytes;
                    if (CreateDataFile()) return srcIndex;
                }
                int writeCount = count - srcIndex;
                long rem = MaxTexBytes - CurrentTexBytes;
                if (writeCount > rem) writeCount = (int)rem;
                DataFile.Write(data, srcIndex, writeCount);
                srcIndex += writeCount;
                CurrentTexBytes += writeCount;
            }
            return count;
        }

        /// <summary>
        /// Writes a byte into texture data file (if current file is full - creates a new one)
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <returns>If byte was wrote or wasn't because of texture file limits</returns>
        public bool PushByte(byte data)
        {
            ThrowIfFinished();
            if (CurrentTexBytes >= MaxTexBytes)
            {
                CurrentTexBytes -= MaxTexBytes;
                if (CreateDataFile()) return false;
            }
            DataFile.WriteByte(data);
            CurrentTexBytes++;
            return true;
        }

        /// <summary>
        /// If limit is not reached, creates a new texture file to write frame data into
        /// </summary>
        /// <returns>If the limit of texture files is reached</returns>
        private bool CreateDataFile()
        {
            DataFile?.Close();
            if (DataFileIndex >= MaxTexCount) return true;
            DataFile = new FileStream(Path.Combine(OutDirPath, $"frameData{DataFileIndex}.dat"), FileMode.Create, FileAccess.Write);
            DataFileIndex++;
            CurrentTexBytes = 0;
            return false;
        }

        public void Finish(string name, int frameWidth, int frameHeight, float fps, int frameCount)
        {
            ThrowIfFinished();
            for (long i = (MaxTexSize << 2) - CurrentTexBytes % (MaxTexSize << 2); i > 0; i--) DataFile.WriteByte(0); // filling the texture so data amount will match it's size
            DataFile.Close();
            StreamWriter file = new(Path.Combine(OutDirPath, $"video.glsl"), false);

            DataFileIndex--;
            long lastTexPixels = CurrentTexBytes >> 2;
            long lastTexWidth = lastTexPixels > MaxTexSize ? MaxTexSize : lastTexPixels;
            long lastTexHeight = (lastTexPixels + MaxTexSize - 1) / MaxTexSize;

            // writing video.glsl
            StringBuilder sb = new();
            sb.AppendLine($"#define name {name}");
            sb.AppendLine($"#define fwidth {frameWidth}");
            sb.AppendLine($"#define fheight {frameHeight}");
            sb.AppendLine($"#define fps {fps}");
            sb.AppendLine($"#define frameCount {frameCount}");
            sb.AppendLine($"#define maxTexSize {MaxTexSize}");
            sb.AppendLine($"#define maxTexPixels {MaxTexSize * MaxTexSize}");
            sb.AppendLine($"#define lastTexWidth {lastTexWidth}");
            sb.AppendLine($"#define lastTexHeight {lastTexHeight}");
            sb.AppendLine();

            // samplers
            for (int i = 0; i <= DataFileIndex; i++) sb.AppendLine($"uniform sampler2D frameData{i};");

            // function for getting a texture vector depending on it's index
            sb.AppendLine($@"
vec4 getTexturePixel(int pixelIndex) {{
    int texIndex = pixelIndex / maxTexPixels;
    pixelIndex %= maxTexPixels; 
    vec2 coord; 
    if (texIndex < {DataFileIndex}) coord = vec2(pixelIndex % maxTexSize + 0.5, pixelIndex / maxTexSize + 0.5) / maxTexSize;
    else coord = vec2(pixelIndex % lastTexWidth + 0.5, pixelIndex / lastTexWidth + 0.5) / vec2(lastTexWidth, lastTexHeight);
    switch(texIndex) {{");
            for (int i = 0; i <= DataFileIndex; i++) sb.AppendLine($"\t\tcase {i}: return texture2D(frameData{i}, coord);");
            sb.AppendLine($@"
    }};
    return vec4(0.0);
}}");
            file.Write(sb.ToString());
            file.Close();

            // writing shaders.properties
            sb.Clear();
            file = new(Path.Combine(OutDirPath, $"shaders.properties"), false);
            for (int i = 0; i < DataFileIndex; i++)
            {
                sb.AppendLine($"texture.gbuffers.frameData{i} = frameData{i}.dat TEXTURE_2D RGBA {MaxTexSize} {MaxTexSize} RGBA UNSIGNED_BYTE");
            }
            sb.AppendLine($"texture.gbuffers.frameData{DataFileIndex} = frameData{DataFileIndex}.dat TEXTURE_2D RGBA {lastTexWidth} {lastTexHeight} RGBA UNSIGNED_BYTE");
            file.Write(sb.ToString());
            file.Close();

            Finished = true;
        }

        private void ThrowIfFinished()
        {
            if (Finished) throw new InvalidOperationException("Video writer is already finalized");
        }
    }
}

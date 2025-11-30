using MSBVPv2.Compiler;
using System.Globalization;

namespace MSBVPv2.CLI;

internal class CLI
{
    private const string ErrorInvalidFormat = "ERROR: Invalid format";
    private const string ErrorOutOfBounds = "ERROR: value out of acceptable bounds";
    private const string ErrorIONotFound = "ERROR: file or directory not found";

    string SrcPath = null!;
    string OutDir = null!;
    int Width = -1;
    int Height = -1;
    float Fps = -1;
    int MaxTexSize = -1;
    int MaxTexCount = -1;

    public async Task RunCLI()
    {
        Console.WriteLine("!!! The program wasn't tested well !!!");

        while (true)
        {
            string input = read("Specify source file path and name with extension");
            if (!File.Exists(input)) Console.WriteLine(ErrorIONotFound);
            else
            {
                SrcPath = input;
                break;
            }
        }

        /*Console.WriteLine("(Dev thing, remove) source is ..\\..\\..\\..\\badapple.mp4");
        SrcPath = "..\\..\\..\\..\\badapple.mp4";*/

        while (true)
        {
            string input = read($"Specify target width - integer in [0..2^31-1]");
            if (!int.TryParse(input, out int w)) Console.WriteLine(ErrorInvalidFormat);
            else if (w < 0) Console.WriteLine(ErrorOutOfBounds);
            else
            {
                Width = w;
                break;
            }
        }

        while (true)
        {
            string input = read("Specify target height - integer in [0..2^31-1]");
            if (!int.TryParse(input, out int h)) Console.WriteLine(ErrorInvalidFormat);
            else if (h < 0) Console.WriteLine(ErrorOutOfBounds);
            else
            {
                Height = h;
                break;
            }
        }

        while (true)
        {
            string input = read("Specify target FPS - float in [0..inf)");
            if (!float.TryParse(input, CultureInfo.InvariantCulture, out float f)) Console.WriteLine(ErrorInvalidFormat);
            else if (f < 0f) Console.WriteLine(ErrorOutOfBounds);
            else
            {
                Fps = f;
                break;
            }
        }

        while (true)
        {
            string input = read("Specify maximum texture size - int in [1..2^31-1]\n" +
                "Values above 8192 are not supported on some modern GPUs, above 4096 - on some GPUs from before 2010\n" +
                "Empty for 4096, **N for Nth power of 2");
            if (string.IsNullOrEmpty(input))
            {
                MaxTexSize = 4096;
                break;
            }
            else if (input.StartsWith("**"))
            {
                input = input[2..];
                if (!int.TryParse(input, out int p)) Console.WriteLine(ErrorInvalidFormat);
                else if (p < 0 || p >= 31) Console.WriteLine(ErrorOutOfBounds);
                else
                {
                    MaxTexSize = 2 << p;
                    break;
                }
            }
            else
            {
                if (!int.TryParse(input, out int s)) Console.WriteLine(ErrorInvalidFormat);
                else if (s <= 0) Console.WriteLine(ErrorOutOfBounds);
                else
                {
                    MaxTexSize = s;
                    break;
                }
            }
        }

        while (true)
        {
            string input = read($"Specify limit of textures - integer in [1..2^31-1]\n(empty for 5)\n" +
                $"Please note that each texture is a separate file and that texture count may be limited too.");
            if (string.IsNullOrEmpty(input))
            {
                MaxTexCount = 5;
                break;
            }
            if (!int.TryParse(input, out int l)) Console.WriteLine(ErrorInvalidFormat);
            else if (l < 1) Console.WriteLine(ErrorOutOfBounds);
            else
            {
                MaxTexCount = l;
                break;
            }
        }

        while (true)
        {
            string str = read("Specify output file directory \n(if left empty - it will be put into same location)\n" +
                "!!! check if it's correct, if there's other video's files, they'll be overwritten !!!");
            if (string.IsNullOrEmpty(str)) str = Path.GetDirectoryName(SrcPath) ?? "";
            if (!Directory.Exists(Path.GetDirectoryName(str))) Console.WriteLine(ErrorIONotFound);
            else
            {
                OutDir = str;
                break;
            }
        }

        /*Console.WriteLine("(Dev thing, remove) outdir is ..\\..\\..\\..\\testOut (press enter)");
        OutDir = "..\\..\\..\\..\\testOut";
        Console.ReadLine();*/

        FrameGetter fg = new(SrcPath, Width, Height, Fps);
        VideoWriter vw = new(MaxTexSize, MaxTexCount, OutDir);
        BitAccumulator bitBuffer = new();
        byte[] buffer = new byte[Width * Height * 1];
        SemaphoreSlim dataLock = new(0);
        int frameCount = 0;

        fg.QueryFrame(bitBuffer);
        Task getFramesTask = Task.Run(() =>
        {
            while (true)
            {
                lock (bitBuffer)
                {
                    if (!fg.QueryFrame(bitBuffer)) break;
                    frameCount++;
                    if ((frameCount & 255) == 0) Console.WriteLine($"{frameCount} frames processed");
                    dataLock.Release();
                }
            }
            Console.WriteLine($"{frameCount} frames processed");
            dataLock.Release();
        });

        Task saveFramesTask = Task.Run(async () =>
        {
            bool run = true;
            while (run)
            {
                await dataLock.WaitAsync();
                lock (bitBuffer)
                {
                    int byteCount = bitBuffer.GetByteCount();
                    for (int index = 0; index < byteCount;)
                    {
                        int bytesRead = bitBuffer.GetBytes(index, buffer, 0, buffer.Length);
                        int bytesWrote = vw.PushBytes(buffer, bytesRead);
                        index += bytesWrote;
                        run = bytesWrote >= bytesRead;
                    }
                    bitBuffer.ClearBytes();
                    if (getFramesTask.IsCompleted)
                    {
                        if (run && bitBuffer.GetExtraBitCount() > 0) vw.PushByte(bitBuffer.GetExtraBits());
                        break;
                    }
                }
            }
        });

        await Task.WhenAll(getFramesTask, saveFramesTask);
        vw.Finish(
            Path.GetFileNameWithoutExtension(SrcPath),
            Width,
            Height,
            Fps,
            frameCount
        );

        Console.WriteLine("Done! Copy those files into [shader_folder]/shaders: video.glsl, shaders.properties and all frameData.dat. Then you can use [shader_folder] as the shader (archive it if you want)");
        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();

        static string read(string prompt)
        {
            Console.WriteLine(prompt);
            Console.Write(">> ");
            return Console.ReadLine() ?? "";
        }
    }

    static async Task Main(string[] args)
    {
        await new CLI().RunCLI();
        // TODO process args
    }
}

using MSBVPv2.Compiler;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Encoder = MSBVPv2.Compiler.Encoder;

namespace MSBVPv2.CLI;

internal class CLI
{
    public string SrcPath = null!;
    public string OutPath = null!;
    public int Width;
    public int Height;
    public float Fps;

    public void RunCLI()
    {
        // TODO write max length
        Console.WriteLine(@"
Before making a shader: some graphic cards don't support long arrays.
For reference: AMD Radeon Vega 8 (2018) supports <CHECK> (may differ with various drivers)
If your shader doesn't load due to some errors - check console for shader compilation errors.
If you see something like:
- Too large array
- Too large shader
- Array size must be a positive integer
- HW_UNSUPPORTED, E_SC_MULTIPLY_DEFINED_LITERAL
Then I can't help with that. It means that the array is too big for your hardware, and you can try using smaller frame dimensions or less fps.
Press ENTER to continue");
        Console.ReadLine();

        while (true)
        {
            string input = Read("Specify source file path and name with extension");
            if (!File.Exists(input)) Console.WriteLine("ERROR: File not found");
            else 
            {
                SrcPath = input;
                break;
            }
        }

        while (true)
        {
            string input = Read("Specify target width - integer in [0..2^31-1]");
            if (!int.TryParse(input, out int w)) Console.WriteLine("ERROR: Invalid format");
            else if (w < 0) Console.WriteLine("ERROR: width must be not negative");
            else
            {
                Width = w;
                break;
            }
        }

        while (true)
        {
            string input = Read("Specify target height - integer in [0..2^31-1]");
            if (!int.TryParse(input, out int h)) Console.WriteLine("ERROR: Invalid format");
            else if (h < 0) Console.WriteLine("ERROR: height must be not negative");
            else
            {
                Height = h;
                break;
            }
        }

        while (true)
        {
            string input = Read("Specify target FPS - float in [0..inf)");
            if (!float.TryParse(input, CultureInfo.InvariantCulture, out float f)) Console.WriteLine("ERROR: Invalid format");
            else if (f < 0f) Console.WriteLine("ERROR: FPS should be not negative");
            else
            {
                Fps = f;
                break;
            }
        }

        while (true)
        {
            string input = Read("Specify output file directory \n(if left empty - it will be put into same location)");
            if (string.IsNullOrEmpty(input)) input = Path.Combine(Path.GetDirectoryName(SrcPath) ?? "", "video.glsl");
            if (!Directory.Exists(Path.GetDirectoryName(input))) Console.WriteLine("Directory doesn't exist");
            else
            {
                OutPath = input;
                break;
            }
        }

        if (File.Exists(OutPath))
        {
            Read($"File {OutPath} already exists, overwrite it? (ENTER - yes, CTRL+C - no)");
        }

        FrameGetter fg = new(SrcPath, Width, Height, Fps);
        ConcurrentQueue<(byte, int)[]> framesToCompress = [];
        SemaphoreSlim frameAvailable = new(0);
        fg.FrameAvailable += (frame) =>
        {
            framesToCompress.Enqueue(frame);
            frameAvailable.Release();
        };
        Task getFramesTask = fg.RunAsync();

        Encoder enc = new();
        ShaderVideoFileWriter outf = new(new FileStream(OutPath, FileMode.Create));
        outf.Name = Path.GetFileNameWithoutExtension(SrcPath);
        outf.Width = Width;
        outf.Height = Height;
        outf.Fps = Fps;

        while (true)
        {
            Task.WaitAny(frameAvailable.WaitAsync(), getFramesTask);
            if (getFramesTask.IsCompleted) break;
            while (framesToCompress.TryDequeue(out var frame))
            {
                int[] fcomp = enc.EncodeFrame(frame);
                outf.PushCompressedFrame(fcomp);
                if ((outf.FrameCount & 0x7F) == 0) Console.WriteLine($"Processed {outf.FrameCount} frames");
            }
        }
        Console.WriteLine("Writing other data...");
        outf.FinalizeFile();
        Console.WriteLine("Done!");
    }

    public static string Read(string prompt)
    {
        Console.WriteLine(prompt);
        Console.Write(">> ");
        return Console.ReadLine() ?? "";
    }

    static void Main(string[] args)
    {
        new CLI().RunCLI();
        // TODO process args
    }
}

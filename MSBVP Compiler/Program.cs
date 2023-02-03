using Emgu.CV;

class Program
{

    // Don't use this code as a guide, I'm not sure it's written well

    static VideoCapture Capture;

    static string path = "";
    static string name = "";

    static int originalFps;
    static int fpsStep;
    static int targetfps;

    static int targetResX;
    static int targetResY;

    static float quality;

    static int frameCount;

    static bool run = true;

    static readonly List<IntVector4> array = new();
    static int bit = 0;
    static int component = 0;

    static StreamWriter saveFile;

    static void Main()
    {
        try
        {
            Console.Write("Source file path: ");
            try
            {
                string? p = Console.ReadLine();
                if (File.Exists(p))
                {
                    Console.WriteLine("Reading the data, please wait...");
                    Capture = new VideoCapture(p);
                }
                else
                {
                    Console.WriteLine("File doesn't exist");
                    return;
                }
                p = p.Replace('\\', '/');
                path = p[0..(p.LastIndexOf('/') + 1)];
                name = p[(p.LastIndexOf('/') + 1)..^0];
            }
            catch
            {
                Console.WriteLine("Unexpected error. The file may be corrupted " +
                    "or the folders specified in the path do not exist.");
                return;
            }

            Console.WriteLine("Original resolution is " + Capture.Width + "x" + Capture.Height);

            Console.Write("Target width: ");
            if (int.TryParse(Console.ReadLine(), out targetResX))
            {
                targetResY = (int)(1f * Capture.Height / Capture.Width * targetResX);

                quality = 1f * targetResY / Capture.Height;
                targetResX = (int)(Capture.Width * quality);

                Console.WriteLine("Target resolution is " + targetResX + "x" + targetResY);
                if (Capture.Width % targetResX != 0)
                    Console.WriteLine("Note: for better result, use a resolution " +
                        "multiple of original");

            }
            else
            {
                Console.WriteLine("Invalid format");
                return;
            }

            Console.Write("Original FPS: ");
            if (int.TryParse(Console.ReadLine(), out int origFps))
            {
                if (origFps <= 0)
                {
                    Console.WriteLine("FPS must be greater than zero.");
                    return;
                }
                else originalFps = origFps;
            }
            else
            {
                Console.WriteLine("Invalid format");
                return;
            }
            Console.Write("Target FPS: ");


            if (int.TryParse(Console.ReadLine(), out int targFps))
            {
                if (targFps <= 0)
                {
                    Console.WriteLine("FPS must be greater than zero.");
                    return;
                }
                else if (targFps > originalFps)
                {
                    Console.WriteLine("Target FPS must be less than original FPS.");
                    return;
                }
                else
                {
                    fpsStep = (int)(1f * originalFps / targFps);
                    targetfps = targFps;
                    if (originalFps % targetfps != 0)
                        Console.WriteLine("Note: for better result, use a fps" +
                            " multiple of original");
                }
            }
            else
            {
                Console.WriteLine("Invalid format");
                return;
            }

            saveFile = new StreamWriter(path + "video.glsl");

            array.Add(IntVector4.Zero);

            while (run)
            {
                ProcessFrame();
            }

            Console.WriteLine("Saving the data...\n");
            if (array.Count > 65000) Console.WriteLine("WARNING: " +
                "The file contains too much data, shader won't compile " +
                "on some devices");

            WriteArray();
            WriteInfo();
            WriteGPFunction();

            saveFile.Close();

            Console.WriteLine("Done!\a File was saved as " + path + "video.glsl");
            Console.WriteLine("(press any key to close)");
            Console.ReadLine();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unknown error: " + e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }

    static void ProcessFrame()
    {
        Mat? frame = null;
        for (int i = 0; i < fpsStep; i++)
            frame = Capture.QueryFrame();

        if (frame is not null)
        {
            byte[] dat = new byte[frame.Cols * frame.Rows * 3];
            Console.WriteLine("Processing frame " + frameCount++);

            frame.CopyTo(dat);

            for (int i = 0; i < targetResY; i++)
            {
                for (int p = 0; p < targetResX; p++)
                {
                    bit++;
                    if (bit == 24)
                    {
                        bit = 0;

                        component++;
                        if (component == 4)
                        {
                            component = 0;
                            array.Add(IntVector4.Zero);
                        }
                    }

                    int pos = (int)(i / quality) * Capture.Width * 3
                        + (int)(p / quality) * 3;
                    if (dat[pos] + dat[pos + 1] + dat[pos + 2] > 384)
                        switch (component)
                        {
                            case 0:
                                array[^1] = array[^1] + IntVector4.UnitX * (1 << bit);
                                break;
                            case 1:
                                array[^1] = array[^1] + IntVector4.UnitY * (1 << bit);
                                break;
                            case 2:
                                array[^1] = array[^1] + IntVector4.UnitZ * (1 << bit);
                                break;
                            case 3:
                                array[^1] = array[^1] + IntVector4.UnitW * (1 << bit);
                                break;
                        }
                }
            }
        }
        else
        {
            run = false;
        }
    }

    static void WriteArray()
    {
        saveFile.WriteLine("const ivec4[" + array.Count + "] data = ivec4["
            + array.Count + "] (\n");
        string res = "";
        foreach (IntVector4 v in array) res += "    " + v.ToString() + ",\n";
        res = res[0..^2];
        saveFile.WriteLine(res + "\n);\n");

    }

    static void WriteGPFunction()
    {
        saveFile.WriteLine(@"
int getPixel(int frame, int offset) {
	int bitpos = frame * framePixels + offset;
	int numpos = bitpos / 24;
	int localbitpos = int(mod(bitpos, 24));
	int vecpos = numpos / 4;
	int localnumpos = int(mod(numpos, 4));
	ivec4 vector = data[vecpos];
	int number = 0;
	if (localnumpos == 0) number = vector.x;
	else if (localnumpos == 1) number = vector.y;
	else if (localnumpos == 2) number = vector.z;
	else number = vector.w;
	return 1 - int(mod(number >> localbitpos, 2));
}"
        );
    }

    static void WriteInfo()
    {
        saveFile.WriteLine(
            "#define name " + name +
            "\n#define fps " + targetfps +
            "\n#define length " + frameCount +
            "\n#define frameWidth " + targetResX +
            "\n#define frameHeight " + targetResY +
            "\nconst int framePixels = frameWidth * frameHeight;");
    }
}

struct IntVector4
{
    public int x;
    public int y;
    public int z;
    public int w;

    public static IntVector4 Zero  = new(0, 0, 0, 0);
    public static IntVector4 UnitX = new(1, 0, 0, 0);
    public static IntVector4 UnitY = new(0, 1, 0, 0);
    public static IntVector4 UnitZ = new(0, 0, 1, 0);
    public static IntVector4 UnitW = new(0, 0, 0, 1);


    public IntVector4(int sx, int sy, int sz, int sw)
    {
        x = sx;
        y = sy;
        z = sz;
        w = sw;
    }

    public static IntVector4 operator +(IntVector4 a, IntVector4 b)
        => new IntVector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);

    public static IntVector4 operator *(IntVector4 a, int b)
        => new IntVector4(a.x * b, a.y * b, a.z * b, a.w * b);

    public override string ToString()
        => string.Format("ivec4({0}, {1}, {2}, {3})", x, y, z, w);
}


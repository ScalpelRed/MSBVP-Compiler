using System.Globalization;

namespace MSBVPv2.CLI;

internal class CLI
{
    private const string ErrorInvalidFormat = "ERROR: Invalid format";
    private const string ErrorOutOfBounds = "ERROR: value out of acceptable bounds";
    private const string ErrorIONotFound = "ERROR: file or directory not found";

    public string SrcPath = null!;
    public string OutPath = null!;
    public int Width = -1;
    public int Height = -1;
    public float Fps = -1;

    public void RunCLI()
    {
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

        while (true)
        {
            string input = read("Specify target width - integer in [0..2^31-1]");
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
            string input = read("Specify output file directory \n(if left empty - it will be put into same location)");
            if (string.IsNullOrEmpty(input)) input = Path.Combine(Path.GetDirectoryName(SrcPath) ?? "", "video.glsl");
            if (!Directory.Exists(Path.GetDirectoryName(input))) Console.WriteLine(ErrorIONotFound);
            else
            {
                OutPath = input;
                break;
            }
        }

        if (File.Exists(OutPath))
        {
            read($"File {OutPath} already exists, overwrite it? (ENTER - yes, CTRL+C - no)");
        }

        

        Console.WriteLine("Press ENTER to exit");
        Console.ReadLine();

        static string read(string prompt)
        {
            Console.WriteLine(prompt);
            Console.Write(">> ");
            return Console.ReadLine() ?? "";
        }
    }
    static void Main(string[] args)
    {

        //new CLI().RunCLI();
        // TODO process args
    }
}

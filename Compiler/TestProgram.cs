using System.Text;

namespace MSBVPv2.Compiler
{
    internal class TestProgram
    {
        static Task Main(string[] args)
        {
            FrameGetter fg = new FrameGetter("..\\..\\..\\..\\badapple.mp4", 72, 54, 4);
            Queue<(byte, int)[]> frames = [];
            fg.FrameAvailable += f =>
            {
                lock (frames)
                {
                    frames.Enqueue(f);
                    Monitor.Pulse(frames);
                }
            };

            Task a = fg.RunAsync();

            Encoder enc = new Encoder();
            while (true)
            {
                while (frames.Count == 0)
                {
                    lock (frames)
                    {
                        Monitor.Wait(frames);
                    }
                }
                var frame = frames.Dequeue();

                byte color = 0;
                int count = 0;
                int colorIndex = 0;
                StringBuilder sb = new();
                for (int y = 0; y < 54; y++)
                {
                    for (int x = 0; x < 72; x++)
                    {
                        if (count <= 0)
                        {
                            color = frame[colorIndex].Item1;
                            count = frame[colorIndex].Item2;
                            colorIndex++;
                        }
                        count--;
                        sb.Append((color > 0) ? "██" : "  ");
                    }
                    sb.AppendLine();
                }
                Console.SetCursorPosition(0, 0);
                Console.Clear();
                Console.WriteLine(sb.ToString());
                Console.WriteLine("================================");
                enc.EncodeFrame(frame);
                Console.ReadLine();
            }
        }
    }
}

using System.Text;

namespace MSBVPv2.Compiler
{
    internal class TestProgram
    {
        static Task Main(string[] args)
        {
            FrameGetter fg = new FrameGetter("..\\..\\..\\..\\badapple.mp4", 36, 48, 4);
            Queue<(int, int)[]> frames = [];
            fg.FrameAvailable += f =>
            {
                lock (frames)
                {
                    frames.Enqueue(f);
                    Monitor.Pulse(frames);
                }
            };

            Task a = fg.RunAsync();
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

                int color = 0;
                int count = 0;
                int colorIndex = 0;
                StringBuilder sb = new();
                for (int y = 0; y < 48; y++)
                {
                    for (int x = 0; x < 36; x++)
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
                Console.WriteLine(sb.ToString());
                Thread.Sleep(25);
            }
        }
    }
}

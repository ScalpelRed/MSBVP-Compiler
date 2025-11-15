using Emgu.CV.BgSegm;
using System.Security.Cryptography;
using System.Text;

namespace MSBVPv2.Compiler
{
    internal class TestProgram
    {
        static Task Main(string[] args)
        {
            int w = 24;
            int h = 18;
            FrameGetter fg = new FrameGetter("..\\..\\..\\..\\badapple.mp4", w, h, 8);
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

            Console.SetWindowSize(100, Console.WindowHeight);
            Console.SetBufferSize(100, Console.BufferHeight);

            int fi = 0;
            while (true)
            {
                while (frames.Count == 0)
                {
                    lock (frames)
                    {
                        Monitor.Wait(frames);
                    }
                }

                (byte color, int count)[] frame;
                lock (frames)
                {
                    frame = frames.Dequeue();
                }

                int[] encf = enc.EncodeFrame(frame);
                //Console.Write(fi + ",");
                foreach (int b in encf) Console.Write(b + ",");
                fi += encf.Length;

                /*{
                    byte color = 0;
                    int count = 0;
                    int groupIndex = 0;
                    StringBuilder sb = new();
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            if (count <= 0)
                            {
                                color = frame[groupIndex].Item1;
                                count = frame[groupIndex].Item2;
                                groupIndex++;
                            }
                            count--;
                            sb.Append((color > 0) ? "█" : " ");
                        }
                        sb.AppendLine();
                    }
                    Console.SetCursorPosition(0, 0);
                    Console.Write(sb.ToString());
                    for (int i = 0; i < w; i++) Console.Write('=');
                }*/

                /*Console.MoveBufferArea(0, 0, w, h+1, 40, 0);
                Console.SetCursorPosition(0, 0);
                enc.EncodeFrame(frame);
                Console.ReadLine();*/
            }
        }
    }
}

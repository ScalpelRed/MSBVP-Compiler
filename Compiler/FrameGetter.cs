using Emgu.CV;
using Emgu.CV.CvEnum;

namespace MSBVPv2.Compiler
{
    public class FrameGetter
    {
        private readonly string FilePath;
        private readonly int TargetWidth;
        private readonly int TargetHeight;
        private readonly float TargetFps;

        private readonly VideoCapture Capture;
        private Mat? Frame;
        private bool NewFrame = true;
        private readonly BitAccumulator LastFrame = new();

        private readonly int FrameStepInt;
        private readonly float FrameStepFrac;
        private float FrameIndFrac = 0;
        private readonly bool SingleFrame;

        public FrameGetter(string filePath, int targetWidth, int targetHeight, float targetFps)
        {
            if (targetWidth < 0) throw new ArgumentException("Target width cannot be negative.", nameof(targetWidth));
            if (targetHeight < 0) throw new ArgumentException("Target height cannot be negative.", nameof(targetHeight));
            if (targetFps < 0) throw new ArgumentException("Target FPS cannot be negative.", nameof(targetFps));

            FilePath = filePath;
            TargetWidth = targetWidth;
            TargetHeight = targetHeight;
            TargetFps = targetFps;

            Capture = new(filePath);

            // frame and pixel position to save are calculated by interpolation used in Bresenham's line algorithm
            float srcFps = (float)Capture.Get(CapProp.Fps);
            FrameStepInt = (int)MathF.Floor(srcFps / targetFps);
            FrameStepFrac = srcFps - FrameStepInt * targetFps;

            if (srcFps == 0 || targetFps == 0) Frame = null;
            else Frame = Capture.QueryFrame();
        }

        public unsafe bool QueryFrame(BitAccumulator dest)
        {
            if (Frame is null) return false;

            if (NewFrame)
            {
                LastFrame.ClearBytes();
                LastFrame.ClearExtraBits();

                int frameWidth = Frame.Cols;
                int frameHeight = Frame.Rows;

                int stepXInt = frameWidth / TargetWidth;
                int stepXFrac = frameWidth - stepXInt * TargetWidth;
                stepXInt *= 3;

                int stepYInt = frameHeight / TargetHeight;
                int stepYFrac = frameHeight - stepYInt * TargetHeight;
                stepYInt *= frameWidth * 3;

                byte* yInt = (byte*)Frame.DataPointer.ToPointer();
                int yFrac = 0;
                for (int y = 0; y < TargetHeight; y++)
                {
                    byte* xInt = yInt;
                    int xFrac = 0;
                    for (int x = 0; x < TargetWidth; x++)
                    {
                        int c = *xInt + *(xInt + 1) + *(xInt + 2);
                        LastFrame.AddBit(c > 384);

                        xInt += stepXInt;
                        xFrac += stepXFrac;
                        if (xFrac >= TargetWidth)
                        {
                            xFrac -= TargetWidth;
                            xInt += 3;
                        }
                    }

                    yInt += stepYInt;
                    yFrac += stepYFrac;
                    if (yFrac >= TargetHeight)
                    {
                        yFrac -= TargetHeight;
                        yInt += frameWidth * 3;
                    }
                }
            }

            dest.AddBits(LastFrame);

            // getting next frame (skipping some by step and one more if fractional part overflows)
            for (int i = 0; i < FrameStepInt && Frame is not null; i++) Frame = Capture.QueryFrame();
            NewFrame = FrameStepInt > 0;
            FrameIndFrac += FrameStepFrac;
            if (FrameIndFrac >= 1f)
            {
                Frame = Capture.QueryFrame();
                NewFrame = true;
                FrameIndFrac -= 1f;
            }

            return true;
        }

        public async Task QueryFrameAsync(BitAccumulator dest)
        {
            await Task.Run(() => QueryFrame(dest));
        }
    }
}

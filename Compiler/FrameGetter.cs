using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace MSBVPv2.Compiler
{
    public class FrameGetter
    {
        private readonly string FilePath;
        private readonly int TargetWidth;
        private readonly int TargetHeight;
        private readonly float TargetFps;
        private readonly IColorSystem TargetColorSystem;

        private readonly VideoCapture Capture;
        private Mat? Frame;
        private bool NewFrame = true;

        private readonly int FrameStepInt;
        private readonly int FrameStepFrac;
        private int FrameIndFrac = 0;

        private readonly byte[] FrameArray;

        public FrameGetter(string filePath, int targetWidth, int targetHeight, float targetFps, IColorSystem targetColorSystem)
        {
            FilePath = filePath;
            TargetWidth = targetWidth;
            TargetHeight = targetHeight;
            TargetFps = targetFps;
            TargetColorSystem = targetColorSystem;

            Capture = new(FilePath);
            Frame = Capture.QueryFrame();

            // frame and pixel position to save are calculated by interpolation used in Bresenham's line algorithm
            float fpsCoef = (float)Capture.Get(CapProp.Fps) / TargetFps;
            FrameStepInt = (int)MathF.Floor(fpsCoef);
            FrameStepFrac = (int)MathF.Floor((fpsCoef - FrameStepInt) * 1000f); // it can't be fully represented with ints, so we'll use precision of 1/1000
            // TODO handle frame steps not fitting int or being zero both

            int outArrayLength = TargetWidth * TargetHeight * targetColorSystem.BitsPerColor;
            outArrayLength = (outArrayLength + 7) >> 3;
            FrameArray = new byte[outArrayLength]; // TODO other color systems
        }

        public bool QueryFrame(BitAccumulator dest)
        {
            if (Frame is null) return false;

            if (NewFrame)
            {
                Image<Rgba, double> image = Frame.ToImage<Rgba, double>(true);
                int frameWidth = Frame.Cols;
                int frameHeight = Frame.Rows;

                int stepXInt = frameWidth / TargetWidth;
                int stepXFrac = frameWidth - stepXInt * TargetWidth;

                int stepYInt = frameHeight / TargetHeight;
                int stepYFrac = frameHeight - stepYInt * TargetHeight;

                int yInt = 0;
                int yFrac = 0;
                for (int y = 0; y < TargetHeight; y++)
                {
                    int xInt = yInt;
                    int xFrac = 0;
                    for (int x = 0; x < TargetWidth; x++)
                    {
                        TargetColorSystem.Encode(
                             image.Data[xInt, yInt, 0], image.Data[xInt, yInt, 1], image.Data[xInt, yInt, 2], image.Data[xInt, yInt, 3],
                             dest
                        );

                        xInt += stepXInt;
                        xFrac += stepXFrac;
                        if (xFrac >= TargetWidth)
                        {
                            xFrac -= TargetWidth;
                            xInt++;
                        }
                    }

                    yInt += stepYInt;
                    yFrac += stepYFrac;
                    if (yFrac >= TargetHeight)
                    {
                        yFrac -= TargetHeight;
                        yInt++;
                    }
                }
            }

            // getting next frame (skipping some by step and one more if fractional part overflows)
            for (int i = 0; i < FrameStepInt && Frame is not null; i++) Frame = Capture.QueryFrame();
            NewFrame = FrameStepInt > 0;
            FrameIndFrac += FrameStepFrac;
            if (FrameIndFrac >= 1000)
            {
                Frame = Capture.QueryFrame();
                NewFrame = true;
                FrameIndFrac -= 1000;
            }

            return true;
        }

        public async Task QueryFrameAsync(BitAccumulator dest)
        {
            await Task.Run(() => QueryFrame(dest));
        }
    }
}

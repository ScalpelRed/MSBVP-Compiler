using Emgu.CV;
using Emgu.CV.CvEnum;

namespace MSBVPv2.Compiler
{
    public class FrameGetter
    {
        private readonly string FilePath;
        private readonly int TargetWidth;
        private readonly int TargetHeight;
        private readonly double TargetFps;

        public FrameGetter(string filePath, int targetWidth, int targetHeight, double targetFps) 
        {
            FilePath = filePath;
            TargetWidth = targetWidth;
            TargetHeight = targetHeight;
            TargetFps = targetFps;
        }

        public void Run()
        {
            VideoCapture capture = new(FilePath);

            // frame and pixel position to save are calculated by interpolation used in Bresenham's line algorithm
            double fpsCoef = capture.Get(CapProp.Fps) / TargetFps;
            int frameStepInt = (int)Math.Floor(fpsCoef);
            int frameStepFrac = (int)Math.Floor((fpsCoef - frameStepInt) * 1000); // it can't be fully represented with ints, so we'll use precision of 1/1000
            int frameIndFrac = 0;

            // indexes of pixel data (3 bytes) in frame array
            int indexStep = -1; // for index in one line
            int indexStepFrac = -1;
            int yIndexStep = -1; // for index at beginning of a line (indexY)
            int yIndexStepFrac = -1;

            Mat? frameMat = capture.QueryFrame();
            bool newFrame = true;
            int frameWidth = -1;
            int frameHeight = -1;
            byte[] frameBuffer = [];

            List<(byte, int)> currentFrame;
            (byte, int)[] lastUniqueFrame = [];

            while (frameMat is not null) // the cycle breaks itself when no more frames
            {
                if (newFrame) // if this frame is different from previous (in meaning that no frames were fetched, there's no actual comparison)
                {
                    if (frameWidth != frameMat.Cols || frameHeight != frameMat.Rows) // if frame size changed, for varying frame size
                    {
                        frameWidth = frameMat.Cols;
                        frameHeight = frameMat.Rows;
                        frameBuffer = new byte[frameWidth * frameHeight * 3];

                        indexStep = frameWidth / TargetWidth; // value needed for fractional step
                        indexStepFrac = frameWidth - indexStep * TargetWidth;
                        indexStep *= 3; // actual stride

                        yIndexStep = frameHeight / TargetHeight; // value needed for fractional step
                        yIndexStepFrac = frameHeight - yIndexStep * TargetHeight;
                        yIndexStep *= 3 * frameWidth; // actual stride
                    }

                    currentFrame = [];
                    frameMat.CopyTo(frameBuffer);
                    byte color = 0;
                    int count = 0;

                    int indexY = 0;
                    int indexYFrac = 0;
                    for (int y = 0; y < TargetHeight; y++)
                    {
                        int index = indexY;
                        int indexFrac = 0;
                        for (int x = 0; x < TargetWidth; x++)
                        {
                            byte pixelColor = (byte)((frameBuffer[index] + frameBuffer[index+1] + frameBuffer[index+2] > 384) ? 1 : 0);
                            if (color == pixelColor) count++;
                            else
                            {
                                if (count > 0) currentFrame.Add((color, count));
                                color = pixelColor;
                                count = 1;
                            }

                            index += indexStep;
                            indexFrac += indexStepFrac; // increasing index
                            if (indexFrac > TargetWidth)
                            {
                                index += 3;
                                indexFrac -= TargetWidth;
                            }
                        }

                        indexY += yIndexStep;
                        indexYFrac += yIndexStepFrac; // increasing indexY
                        if (indexYFrac > TargetHeight)
                        {
                            indexY += frameWidth * 3;
                            indexYFrac -= TargetHeight;
                        }
                    }
                    currentFrame.Add((color, count)); // adding the last pixel group (we didn't add it in xy-cycle)
                    lastUniqueFrame = currentFrame.ToArray();
                    FrameAvailable?.Invoke(lastUniqueFrame);
                }
                else
                {
                    // if no frames fetched, but we need one more - we return the previous one
                    FrameAvailable?.Invoke(lastUniqueFrame);
                }

                // getting next frame (skipping some by step and one more if fractional part overflows)
                for (int i = 0; i < frameStepInt; i++) frameMat = capture.QueryFrame();
                newFrame = frameStepInt > 0;
                frameIndFrac += frameStepFrac;
                if (frameIndFrac >= 1000)
                {
                    frameMat = capture.QueryFrame();
                    newFrame = true;
                    frameIndFrac -= 1000;
                }
            }
        }

        public async Task RunAsync()
        {
            await Task.Run(Run);
        }

        public event Action<(byte color, int count)[]>? FrameAvailable;

    }
}

using Emgu.CV.BgSegm;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Security.Cryptography;

namespace MSBVPv2.Compiler
{
    public class Encoder
    {
        // There are 4 types of compressed groups:
        // 0) 2  inner groups, 14 count bits (up to ~16K pixels) each
        // 1) 3  inner groups,  9 count bits (up to  512 pixels) each
        // 2) 6  inner groups,  4 count bits (up to   16 pixels) each
        // 3) 30 inner groups,  0 count bits each (exactly 30 pixels) (aka immediate colors)

        // Seems like this is less effective than expected, maybe will change it

        private static readonly CompressedGroupInfo[] CompressedGroups =
        [
            new CompressedGroupInfo(2),
            new CompressedGroupInfo(3),
            new CompressedGroupInfo(6),
            new CompressedGroupInfo(30),
        ];

        public Encoder()
        {

        }

        public int[] EncodeFrame((byte color, int count)[] frame)
        {
            if (frame.Length <= 0) return [ ];
            List<int> res = [];
            int groupIndex = 0;
            (byte color, int count) group = frame[groupIndex];
            while (groupIndex < frame.Length)
            {
                int maxGroups = 0;
                int maxGroupsComp = 0;
                for (int ci = CompressedGroups.Length - 1; ci >= 0; ci--)
                {
                    int lookupGroupIndex = groupIndex;
                    int groupCount = 0;
                    int pixelCount = 0;
                    CompressedGroupInfo cg = CompressedGroups[ci];
                    while (pixelCount < cg.MaxPixelsTotal)
                    {
                        if (groupCount >= cg.GroupCount || lookupGroupIndex >= frame.Length) break;
                        pixelCount += frame[lookupGroupIndex].count;
                        groupCount++;
                        lookupGroupIndex++;
                    }
                    if (groupCount >= maxGroups)
                    {
                        maxGroups = groupCount;
                        maxGroupsComp = ci;
                    }
                }

                CompressedGroupInfo comp = CompressedGroups[maxGroupsComp];
                int shift = 0;
                int resInt = 0;
                for (int i = 0; i < comp.GroupCount; i++)
                {
                    int pixelsToPut = group.count;
                    if (comp.MaxPixelsPerGroup < pixelsToPut) pixelsToPut = comp.MaxPixelsPerGroup;

                    resInt |= (pixelsToPut - 1) << shift;
                    shift += comp.CountBitsPerGroup;
                    resInt |= (group.color & 0b1) << shift;
                    shift++;

                    group.count -= pixelsToPut;
                    if (group.count <= 0)
                    {
                        groupIndex++;
                        if (groupIndex < frame.Length) group = frame[groupIndex];
                        else break;
                    }
                }
                resInt |= maxGroupsComp << 30;
                res.Add(resInt);

                // TODO remove debug
                /*Console.WriteLine(Convert.ToString(resInt, 2).PadLeft(32, '0'));
                char c1 = '█';
                char c2 = ' ';
                Console.Write("██");
                for (int k = comp.GroupCount - 1; k >= 0; k--)
                {
                    (c1, c2) = (c2, c1);
                    for (int p = comp.CountBitsPerGroup; p >= 0; p--) Console.Write(c1);
                }
                Console.WriteLine();*/
            }
            /*for (int i = 0; i < 20; i++) Console.WriteLine("                                ");
            Console.SetCursorPosition(0, 0);*/
            return res.ToArray();
        }

        public string GetDecodeGLSLFuncBody()
        {
            throw new NotImplementedException(); // TODO
        }

        private class CompressedGroupInfo
        {
            public readonly int GroupCount;
            public readonly int CountBitsPerGroup;
            public readonly int MaxPixelsPerGroup;
            public readonly int MaxPixelsTotal;

            public CompressedGroupInfo(int groupCount)
            {
                GroupCount = groupCount;
                CountBitsPerGroup = 30 / groupCount - 1;
                MaxPixelsPerGroup = 1 << CountBitsPerGroup;
                MaxPixelsTotal = MaxPixelsPerGroup * GroupCount;
            }
        }
    }
}

namespace MSBVPv2.Compiler
{
    // Could be improved, but maybe later, for now it does just what needed
    public class BitAccumulator
    {
        private readonly List<byte> FullBytes = [];
        private byte ExtraBits = 0;
        private int ExtraBitCount = 0;

        public BitAccumulator()
        {

        }

        public void AddBits(BitAccumulator bits)
        {
            byte[] array = new byte[bits.GetByteCount()];
            bits.GetBytes(0, array, 0, array.Length);
            AddBits(array);
            AddBits(bits.ExtraBits, bits.ExtraBitCount);
        }

        public unsafe void AddBits<T>(T value, int bitCount) where T : unmanaged
        {
            ThrowIfBitCountInvalid<T>(bitCount);
            byte bits = ExtraBits;
            int shiftAmount = ExtraBitCount;
            byte* ptr = (byte*)&value;
            for (; bitCount >= 8; bitCount -= 8)
            {
                bits |= (byte)((*ptr << shiftAmount) & 0xFF);
                FullBytes.Add(bits);
                bits = (byte)(*ptr >> (8 - shiftAmount));
                ptr += 1;
            }
            if (bitCount > 0)
            {
                bits |= (byte)((*ptr << bitCount) & 0xFF);
                shiftAmount += bitCount;
                if (shiftAmount >= 8)
                {
                    shiftAmount -= 8;
                    FullBytes.Add(bits);
                    bits |= (byte)(*ptr >> (8 - shiftAmount));
                }
            }
            ExtraBits = bits;
            ExtraBitCount = shiftAmount;
        }

        public void AddBits(byte[] data, int bitsInLast = 8) // TODO simplify, mostly repeats method above
        {
            if (data.Length == 0) return;
            int bitCount = ((data.Length - 1) << 3) + bitsInLast;
            byte bits = ExtraBits;
            int shiftAmount = ExtraBitCount;
            int byteIndex = 0;
            for (; bitCount >= 8; bitCount -= 8)
            {
                byte b = data[byteIndex];
                bits |= (byte)((b << shiftAmount) & 0xFF);
                FullBytes.Add(bits);
                bits = (byte)(b >> (8 - shiftAmount));
                byteIndex++;
            }
            if (bitCount > 0)
            {
                byte b = data[byteIndex];
                bits |= (byte)((b << bitCount) & 0xFF);
                shiftAmount += bitCount;
                if (shiftAmount >= 8)
                {
                    shiftAmount -= 8;
                    FullBytes.Add(bits);
                    bits |= (byte)(b >> (8 - shiftAmount));
                }
            }
            ExtraBits = bits;
            ExtraBitCount = shiftAmount;
        }

        public void AddBits(byte value, int bitCount)
        {
            ThrowIfBitCountInvalid<byte>(bitCount);
            if (bitCount == 0) return;
            ExtraBits |= (byte)((value << ExtraBitCount) & 0xFF);
            ExtraBitCount += bitCount;
            if (ExtraBitCount >= 8)
            {
                ExtraBitCount -= 8;
                FullBytes.Add(ExtraBits);
                ExtraBits = (byte)(value >> (8 - ExtraBitCount));
            }
        }

        public void AddBit(bool value)
        {
            if (value) ExtraBits |= (byte)(1 << ExtraBitCount); // TODO  remove. branching.
            ExtraBitCount++;
            if (ExtraBitCount >= 8)
            {
                ExtraBitCount -= 8;
                FullBytes.Add(ExtraBits);
                ExtraBits = 0;
            }
        }

        public void AddBit(byte value)
        {
            ExtraBits |= (byte)((value & 0x1) << ExtraBitCount);
            ExtraBitCount++;
            if (ExtraBitCount >= 8)
            {
                ExtraBitCount -= 8;
                FullBytes.Add(ExtraBits);
                ExtraBits = 0;
            }
        }

        private static unsafe void ThrowIfBitCountInvalid<T>(int bitCount, int bitsGot = -1) where T : unmanaged
        {
            if (bitsGot < 0) bitsGot = sizeof(T) << 3;
            if (bitCount < 0) throw new ArgumentException("Bit count cannot be negative");
            if (bitCount > bitsGot) throw new ArgumentException($"An object of type {typeof(T).Name} doesn't have {bitCount} bits");
        }

        public int GetByteCount() => FullBytes.Count;

        public int GetBytes(int srcIndex, byte[] dest, int destIndex, int count)
        {
            if (count > FullBytes.Count - srcIndex) count = FullBytes.Count - srcIndex;
            FullBytes.CopyTo(srcIndex, dest, destIndex, count);
            return count;
        }

        public void ClearBytes()
        {
            FullBytes.Clear();
        }

        public void ClearBytes(int amount)
        {
            FullBytes.RemoveRange(0, amount);
        }

        public int GetExtraBitCount() => ExtraBitCount;

        public byte GetExtraBits() => ExtraBits;

        public void ClearExtraBits()
        {
            ExtraBits = 0;
            ExtraBitCount = 0;
        }

        public bool IsEmpty() => (FullBytes.Count == 0) && (ExtraBitCount == 0);
    }
}

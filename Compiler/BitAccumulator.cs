namespace MSBVPv2.Compiler
{
    // Could be improved, but maybe later, for now it does just what needed
    public class BitAccumulator
    {
        private readonly List<byte> FullBytes = [];
        private byte ExtraBits = 0;
        private int ExtraBitCount = 0;
        private readonly object bytesLock = new();

        public BitAccumulator()
        {
            
        }

        public unsafe void AddBits<T>(T value, int bitCount) where T : unmanaged
        {
            ThrowIfBitCountInvalid<T>(bitCount);
            lock (bytesLock)
            {
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
        }

        public void AddBits(byte value, int bitCount)
        {
            ThrowIfBitCountInvalid<byte>(bitCount);
            if (bitCount == 0) return;
            lock (bytesLock)
            {
                ExtraBits |= (byte)((value << ExtraBitCount) & 0xFF);
                ExtraBitCount += bitCount;
                if (ExtraBitCount >= 8)
                {
                    ExtraBitCount -= 8;
                    FullBytes.Add(ExtraBits);
                    ExtraBits = (byte)(value >> (8 - ExtraBitCount));
                }
            }
        }

        public void AddBit(bool value)
        {
            lock (bytesLock)
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
        }

        public void AddBit(byte value) 
        {
            lock (bytesLock)
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
        }

        private static unsafe void ThrowIfBitCountInvalid<T>(int bitCount) where T : unmanaged
        {
            if (bitCount < 0) throw new ArgumentException("Bit count cannot be negative");
            if (bitCount > (sizeof(T) << 3)) throw new ArgumentException($"An object of type {typeof(T).Name} doesn't have {bitCount} bits");
        }

        public int GetByteCount()
        {
            lock (bytesLock)
            {
                return FullBytes.Count;
            }
        }

        public void GetBytes(byte[] dest, int index, int count)
        {
            lock (bytesLock)
            {
                FullBytes.CopyTo(0, dest, index, count);
            }
        }

        public void Clear()
        {
            lock (bytesLock)
            {
                FullBytes.Clear();
            }
        }
    }
}

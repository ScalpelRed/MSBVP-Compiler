namespace MSBVPv2.Compiler
{
    public interface IColorSystem
    {
        int BitsPerColor { get; }

        void Encode(double r, double g, double b, double a, BitAccumulator dest);
    }
}

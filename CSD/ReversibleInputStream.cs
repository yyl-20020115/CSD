namespace CSD;

public class ReversibleInputStream(byte[] data)
{
    public readonly byte[] data = data;
    public int index = 0;
    public void ResetCounter() => index = 0;
    public int Counter => index;
    public long Read(long bits) => bits switch
    {
        8 => data[index++],
        16 => Read16(),
        32 => Read32(),
        64 => (long)Read32() | (long)Read32() << 32,
        _ => throw new InvalidOperationException("unimplemented read amount " + bits)
    };

    public int Read32() => (data[index++] & 0xFF) | ((data[index++] & 0xFF) << 8) | ((data[index++] & 0xFF) << 16) | ((data[index++] & 0xFF) << 24);

    public int Read16() => (data[index++] & 0xFF) | (data[index++] << 8);

    public int Byte => data[index] & 0xFF;

    public void Forward() => index++;

    public void Reverse() => index--;
}

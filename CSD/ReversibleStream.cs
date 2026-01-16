namespace CSD;

public class ReversibleStream(byte[] data)
{
    public static implicit operator ReversibleStream(byte[] data) => new(data);
    public readonly byte[] Data = data;
    public long Index = 0;
    public void Reset() => Index = 0;
    public long Read(long bits) => bits switch
    {
        8 => Data[this.Index++],
        16 => Read16(),
        32 => Read32(),
        64 => (long)Read32() | (long)Read32() << 32,
        _ => throw new InvalidOperationException($"unimplemented read amount {bits}")
    };

    public int Read32() => (Data[Index++] & 0xFF) 
        | ((Data[Index++] & 0xFF) << 8) 
        | ((Data[Index++] & 0xFF) << 16) 
        | ((Data[Index++] & 0xFF) << 24)
        ;

    public int Read16() => (Data[this.Index++] & 0xFF) 
        | (Data[this.Index++] << 8)
        ;

    public int Current => Data[this.Index];

    public void Forward() => this.Index++;

    public void Backward() => this.Index--;
}

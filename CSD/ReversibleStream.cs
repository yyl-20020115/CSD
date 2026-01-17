namespace CSD;

public class ReversibleStream(byte[] data)
{
    public static implicit operator ReversibleStream(byte[] data)
        => new(data);
    public readonly byte[] Data = data;
    public long Index = 0;
    public void Reset()
        => this.Index = 0;
    public long Read(long bits) => bits switch
    {
        08 => this.Read8(),
        16 => this.Read16(),
        32 => this.Read32(),
        64 => (long)this.Read32()
            | (long)this.Read32() << 32,
        _ => throw new InvalidOperationException(
            $"unimplemented read amount {bits}")
    };

    public int Read32()
       => this.Index < this.Data.Length - 3 ?
        ((this.Data[this.Index++] & 0xFF) << 00)
        |((this.Data[this.Index++] & 0xFF) << 08)
        |((this.Data[this.Index++] & 0xFF) << 16)
        |((this.Data[this.Index++] & 0xFF) << 24)
        :0
        ;

    public int Read8()
       => this.Index < this.Data.Length ?
        (this.Data[this.Index++] & 0xFF)
        : 0
        ;

    public int Read16()
       => this.Index < this.Data.Length - 1 ?
        ((this.Data[this.Index++] & 0xFF)
        |(this.Data[this.Index++] << 08))
        :0
        ;

    public int Current
        => this.Index < this.Data.Length
        ? this.Data[this.Index] : 0
        ;

    public void Forward()
    {
        if (this.Index < this.Data.Length - 1)
        {
            ++this.Index;
        }
    }

    public void Backward()
    {
        if (this.Index > 0)
        {
            --this.Index;
        }
    }
}

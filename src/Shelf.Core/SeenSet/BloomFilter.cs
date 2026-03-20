using System.Collections;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;

namespace Shelf.Core.SeenSet;

public sealed class BloomFilter
{
    private readonly BitArray _bits;
    private readonly int _bitCount;
    private readonly int _hashCount;
    private readonly uint[] _seeds;

    public BloomFilter(int expectedItems, double falsePositiveRate = 0.01)
    {
        if (expectedItems <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedItems), "Expected items must be positive");
        if (falsePositiveRate is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(falsePositiveRate), "False positive rate must be between 0 and 1");

        _bitCount = CalculateOptimalBitCount(expectedItems, falsePositiveRate);
        _hashCount = CalculateOptimalHashCount(expectedItems, _bitCount);
        _bits = new BitArray(_bitCount);
        _seeds = GenerateSeeds(_hashCount);
    }

    private BloomFilter(int bitCount, int hashCount, BitArray bits)
    {
        _bitCount = bitCount;
        _hashCount = hashCount;
        _bits = bits;
        _seeds = GenerateSeeds(_hashCount);
    }

    public int BitCount => _bitCount;
    public int HashCount => _hashCount;
    public int Count { get; private set; }

    public void Add(string item)
    {
        var bytes = Encoding.UTF8.GetBytes(item);

        for (int i = 0; i < _hashCount; i++)
        {
            var hash = XxHash64.HashToUInt64(bytes.AsSpan(), _seeds[i]);
            var index = (int)(hash % (ulong)_bitCount);
            _bits[index] = true;
        }

        Count++;
    }

    public bool Contains(string item)
    {
        var bytes = Encoding.UTF8.GetBytes(item);

        for (int i = 0; i < _hashCount; i++)
        {
            var hash = XxHash64.HashToUInt64(bytes.AsSpan(), _seeds[i]);
            var index = (int)(hash % (ulong)_bitCount);

            if (!_bits[index])
                return false;
        }

        return true;
    }

    public double EstimatedFalsePositiveRate()
    {
        if (Count == 0)
            return 0;

        var exponent = -(double)_hashCount * Count / _bitCount;
        var base1 = 1 - Math.Exp(exponent);
        return Math.Pow(base1, _hashCount);
    }

    public double FillRatio()
    {
        int setBits = 0;
        for (int i = 0; i < _bitCount; i++)
        {
            if (_bits[i])
                setBits++;
        }
        return (double)setBits / _bitCount;
    }

    public void Save(string bloomPath, string metadataPath)
    {
        var dir = Path.GetDirectoryName(bloomPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Write bit array as raw bytes
        var byteCount = (_bitCount + 7) / 8;
        var bytes = new byte[byteCount];
        for (int i = 0; i < _bitCount; i++)
        {
            if (_bits[i])
                bytes[i / 8] |= (byte)(1 << (i % 8));
        }
        File.WriteAllBytes(bloomPath, bytes);

        // Write metadata
        var metadata = new BloomMetadata(_bitCount, _hashCount, Count);
        var json = JsonSerializer.Serialize(metadata, BloomMetadataContext.Default.BloomMetadata);
        File.WriteAllText(metadataPath, json);
    }

    public static BloomFilter? Load(string bloomPath, string metadataPath)
    {
        if (!File.Exists(bloomPath) || !File.Exists(metadataPath))
            return null;

        var json = File.ReadAllText(metadataPath);
        var metadata = JsonSerializer.Deserialize(json, BloomMetadataContext.Default.BloomMetadata);
        if (metadata is null)
            return null;

        var bytes = File.ReadAllBytes(bloomPath);
        var bits = new BitArray(metadata.BitCount);
        for (int i = 0; i < metadata.BitCount; i++)
        {
            bits[i] = (bytes[i / 8] & (1 << (i % 8))) != 0;
        }

        return new BloomFilter(metadata.BitCount, metadata.HashCount, bits) { Count = metadata.Count };
    }

    private static int CalculateOptimalBitCount(int expectedItems, double falsePositiveRate)
    {
        var bitCount = -(expectedItems * Math.Log(falsePositiveRate)) / (Math.Log(2) * Math.Log(2));
        return (int)Math.Ceiling(bitCount);
    }

    private static int CalculateOptimalHashCount(int expectedItems, int bitCount)
    {
        var hashCount = ((double)bitCount / expectedItems) * Math.Log(2);
        return Math.Max(1, (int)Math.Round(hashCount));
    }

    private static uint[] GenerateSeeds(int count)
    {
        var seeds = new uint[count];
        var random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < count; i++)
        {
            seeds[i] = (uint)random.Next();
        }

        return seeds;
    }
}

public sealed record BloomMetadata(int BitCount, int HashCount, int Count);

[System.Text.Json.Serialization.JsonSerializable(typeof(BloomMetadata))]
internal partial class BloomMetadataContext : System.Text.Json.Serialization.JsonSerializerContext { }

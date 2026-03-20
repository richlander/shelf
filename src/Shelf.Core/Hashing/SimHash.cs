using System.IO.Hashing;
using System.Numerics;
using System.Text;

namespace Shelf.Core.Hashing;

public sealed class SimHash
{
    private readonly int _hashBits;

    public SimHash(int hashBits = 64)
    {
        if (hashBits is <= 0 or > 64)
            throw new ArgumentOutOfRangeException(nameof(hashBits), "Hash bits must be between 1 and 64");

        _hashBits = hashBits;
    }

    public int HashBits => _hashBits;

    public ulong ComputeSignature(IEnumerable<string> features)
    {
        var fingerprint = new int[_hashBits];

        foreach (var feature in features)
        {
            var bytes = Encoding.UTF8.GetBytes(feature);
            var hash = XxHash64.HashToUInt64(bytes.AsSpan());

            for (int i = 0; i < _hashBits; i++)
            {
                var bit = (hash >> i) & 1;
                fingerprint[i] += bit == 1 ? 1 : -1;
            }
        }

        ulong signature = 0;
        for (int i = 0; i < _hashBits; i++)
        {
            if (fingerprint[i] > 0)
                signature |= (1UL << i);
        }

        return signature;
    }

    public ulong ComputeSignature(string text)
    {
        var tokens = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        return ComputeSignature(tokens);
    }

    public int HammingDistance(ulong a, ulong b) =>
        BitOperations.PopCount(a ^ b);

    public double Similarity(ulong a, ulong b) =>
        1.0 - (double)HammingDistance(a, b) / _hashBits;

    public double Similarity(string a, string b) =>
        Similarity(ComputeSignature(a), ComputeSignature(b));
}

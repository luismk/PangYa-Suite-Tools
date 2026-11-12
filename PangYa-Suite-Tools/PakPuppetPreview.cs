using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Windows.Media.Media3D;

namespace PangYa_Suite_Tools;

internal sealed record PakPuppetPreview(string[] Sections, string[] Textures,
    Point3D[] Positions, PakPuppetPreview.Corner[] Corners, byte[] Materials)
{
    internal readonly record struct Corner(int Vertex, Vector3D Normal, System.Windows.Point UV);

    // Layout references: docs.pangya.golf/pc/file-formats/pet and
    // github.com/Acrisio-Filho/pet-source_tools/blob/master/pet.py (MPET-specific fields).
    public static PakPuppetPreview Parse(byte[] data, bool isMpet, Encoding encoding, CancellationToken token)
    {
        var sections = new Dictionary<string, (int Offset, int Length)>(StringComparer.Ordinal);
        int offset = 0;
        while (offset < data.Length)
        {
            if (data.Length - offset < 8 || sections.Count >= 256)
                throw new InvalidDataException();
            string id = Encoding.ASCII.GetString(data, offset, 4);
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4));
            offset += 8;
            if (size > data.Length - offset || !sections.TryAdd(id, (offset, (int)size)))
                throw new InvalidDataException();
            offset += (int)size;
        }

        if (sections.Count == 0)
            throw new InvalidDataException();

        int minor = 0;
        if (sections.TryGetValue("VERS", out var version))
        {
            if (version.Length < 2 || data[version.Offset + 1] != 1 || data[version.Offset] > 3)
                throw new InvalidDataException();
            minor = data[version.Offset];
        }

        string[] textures = [];
        if (sections.TryGetValue("TEXT", out var textureSection))
        {
            using var reader = OpenSection(data, textureSection);
            int count = ReadCount(reader, 44, 256);
            textures = new string[count];
            for (int i = 0; i < count; i++)
            {
                byte[] name = reader.ReadBytes(32);
                int end = Array.IndexOf(name, (byte)0);
                textures[i] = encoding.GetString(name, 0, end < 0 ? name.Length : end);
                Skip(reader, 12);
            }
        }

        if (!sections.TryGetValue("MESH", out var mesh))
            return new PakPuppetPreview(sections.Keys.ToArray(), textures, [], [], []);

        using var input = OpenSection(data, mesh);
        if (isMpet)
            Skip(input, input.ReadByte() * 20);

        int vertexCount = ReadCount(input, isMpet ? 20 : 16, 250_000);
        var positions = new Point3D[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            token.ThrowIfCancellationRequested();
            positions[i] = new Point3D(ReadFinite(input), ReadFinite(input), ReadFinite(input));
            if (isMpet)
                Skip(input, 4); // Global bone weight.
            ReadBoneWeights(input);
        }

        int faces = ReadCount(input, minor >= 2 ? 53 : 73, 250_000);
        var corners = new Corner[faces * 3];
        for (int i = 0; i < corners.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            uint vertex = input.ReadUInt32();
            if (vertex >= vertexCount)
                throw new InvalidDataException();
            var normal = new Vector3D(ReadFinite(input), ReadFinite(input), ReadFinite(input));
            int uvCount = minor >= 2 ? input.ReadByte() : 1;
            System.Windows.Point uv = default;
            if (uvCount > 0)
            {
                uv = new System.Windows.Point(ReadFinite(input), ReadFinite(input));
                Skip(input, (uvCount - 1) * 8);
            }
            corners[i] = new Corner((int)vertex, normal, uv);
        }
        byte[] materials = input.ReadBytes(faces);
        if (materials.Length != faces)
            throw new EndOfStreamException();
        if (minor >= 2)
            Skip(input, faces); // Specular material indices.

        return new PakPuppetPreview(sections.Keys.ToArray(), textures, positions, corners, materials);
    }

    private static void ReadBoneWeights(BinaryReader reader)
    {
        int sum = 0;
        int count = 0;
        while (sum < 255)
        {
            if (++count > 256)
                throw new InvalidDataException();
            sum += reader.ReadByte();
            if (reader.ReadByte() == 0xFE)
                Skip(reader, 2);
        }
        if (sum != 255)
            throw new InvalidDataException();
        if (count == 1)
            Skip(reader, 2);
    }

    private static BinaryReader OpenSection(byte[] data, (int Offset, int Length) section) =>
        new(new MemoryStream(data, section.Offset, section.Length, writable: false));

    private static int ReadCount(BinaryReader reader, int minimumSize, int limit)
    {
        uint count = reader.ReadUInt32();
        if (count > limit || count > (reader.BaseStream.Length - reader.BaseStream.Position) / minimumSize)
            throw new InvalidDataException();
        return (int)count;
    }

    private static void Skip(BinaryReader reader, int length)
    {
        if (length > reader.BaseStream.Length - reader.BaseStream.Position)
            throw new EndOfStreamException();
        reader.BaseStream.Position += length;
    }

    private static float ReadFinite(BinaryReader reader)
    {
        float value = reader.ReadSingle();
        return float.IsFinite(value) ? value : throw new InvalidDataException();
    }
}

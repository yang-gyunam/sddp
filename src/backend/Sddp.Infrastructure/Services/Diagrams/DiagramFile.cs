using Sddp.Abstractions.Models.Diagrams;
using System.Text;

namespace Sddp.Infrastructure.Services.Diagrams;

public static class DiagramFile
{
    public static DiagramRevision Read(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new InvalidDataException("Expected a diagram YAML file path.");
        return DiagramYaml.Read(Decode(File.ReadAllBytes(path)));
    }

    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        // Match the BOMs previously recognized by File.ReadAllText, using strict decoders.
        Encoding encoding = bytes switch
        {
            [0, 0, 0xfe, 0xff, ..] => new UTF32Encoding(true, true, true),
            [0xff, 0xfe, 0, 0, ..] => new UTF32Encoding(false, true, true),
            [0xfe, 0xff, ..] => new UnicodeEncoding(true, true, true),
            [0xff, 0xfe, ..] => new UnicodeEncoding(false, true, true),
            [0xef, 0xbb, 0xbf, ..] => new UTF8Encoding(true, true),
            _ => new UTF8Encoding(false, true)
        };
        try
        {
            return encoding.GetString(bytes[encoding.Preamble.Length..]);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException($"Invalid {encoding.WebName} in diagram YAML.", ex);
        }
    }
}

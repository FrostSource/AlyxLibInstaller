using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDeployment.Extensions;
public static class StreamExtensions
{
    /// <summary>
    /// Writes the specified text to the provided stream using the specified encoding.
    /// </summary>
    /// <remarks>This method leaves the stream open after writing. Ensure the stream is properly closed or
    /// disposed elsewhere if necessary.</remarks>
    /// <param name="stream">The stream to which the text will be written. Must be writable.</param>
    /// <param name="text">The text to write to the stream.</param>
    /// <param name="encoding">The character encoding to use. If <see langword="null"/>, UTF-8 encoding is used by default.</param>
    public static void WriteText(this Stream stream, string text, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        using var writer = new StreamWriter(stream, encoding, leaveOpen: true);
        writer.Write(text);
        writer.Flush();
    }
}

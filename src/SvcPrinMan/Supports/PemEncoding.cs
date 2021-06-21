// Extracted from https://github.com/dotnet/runtime/blob/main/src/libraries/System.Security.Cryptography.Encoding/src/System/Security/Cryptography/PemEncoding.cs due to Azure Functions .NET 5.0 compability issues
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Provides methods for reading and writing the IETF RFC 7468
    /// subset of PEM (Privacy-Enhanced Mail) textual encodings.
    /// This class cannot be inherited.
    /// </summary>
    public static class PemEncoding
    {
        private const string PreEBPrefix = "-----BEGIN ";
        private const string PostEBPrefix = "-----END ";
        private const string Ending = "-----";
        private const int EncodedLineLength = 64;

        /// <summary>
        /// Determines the length of a PEM-encoded value, in characters,
        /// given the length of a label and binary data.
        /// </summary>
        /// <param name="labelLength">
        /// The length of the label, in characters.
        /// </param>
        /// <param name="dataLength">
        /// The length of the data, in bytes.
        /// </param>
        /// <returns>
        /// The number of characters in the encoded PEM.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="labelLength"/> is a negative value.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="dataLength"/> is a negative value.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="labelLength"/> exceeds the maximum possible label length.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="dataLength"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The length of the PEM-encoded value is larger than <see cref="int.MaxValue"/>.
        /// </exception>
        public static int GetEncodedSize(int labelLength, int dataLength)
        {
            int preebLength = PreEBPrefix.Length + labelLength + Ending.Length;
            int postebLength = PostEBPrefix.Length + labelLength + Ending.Length;
            int totalEncapLength = preebLength + postebLength + 1; //Add one for newline after preeb

            // dataLength is already known to not overflow here
            int encodedDataLength = ((dataLength + 2) / 3) << 2;
            int lineCount = Math.DivRem(encodedDataLength, EncodedLineLength, out int remainder);

            if (remainder > 0)
                lineCount++;

            int encodedDataLengthWithBreaks = encodedDataLength + lineCount;

            return encodedDataLengthWithBreaks + totalEncapLength;
        }

        /// <summary>
        /// Tries to write the provided data and label as PEM-encoded data into
        /// a provided buffer.
        /// </summary>
        /// <param name="label">
        /// The label to write.
        /// </param>
        /// <param name="data">
        /// The data to write.
        /// </param>
        /// <param name="destination">
        /// The buffer to receive the PEM-encoded text.
        /// </param>
        /// <param name="charsWritten">
        /// When this method returns, this parameter contains the number of characters
        /// written to <paramref name="destination"/>. This parameter is treated
        /// as uninitialized.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="destination"/> is large enough to contain
        /// the PEM-encoded text, otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method always wraps the base-64 encoded text to 64 characters, per the
        /// recommended wrapping of IETF RFC 7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="label"/> exceeds the maximum possible label length.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="data"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The resulting PEM-encoded text is larger than <see cref="int.MaxValue"/>.
        ///   <para>
        ///       - or -
        ///   </para>
        /// <paramref name="label"/> contains invalid characters.
        /// </exception>
        public static bool TryWrite(ReadOnlySpan<char> label, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten)
        {
            static int Write(ReadOnlySpan<char> str, Span<char> dest, int offset)
            {
                str.CopyTo(dest.Slice(offset));
                return str.Length;
            }

            static int WriteBase64(ReadOnlySpan<byte> bytes, Span<char> dest, int offset)
            {
                bool success = Convert.TryToBase64Chars(bytes, dest.Slice(offset), out int base64Written);

                if (!success)
                {
                    Debug.Fail("Convert.TryToBase64Chars failed with a pre-sized buffer");
                    throw new ArgumentException(null, nameof(destination));
                }

                return base64Written;
            }

            const string NewLine = "\n";
            const int BytesPerLine = 48;
            int encodedSize = GetEncodedSize(label.Length, data.Length);

            if (destination.Length < encodedSize)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = 0;
            charsWritten += Write(PreEBPrefix, destination, charsWritten);
            charsWritten += Write(label, destination, charsWritten);
            charsWritten += Write(Ending, destination, charsWritten);
            charsWritten += Write(NewLine, destination, charsWritten);

            ReadOnlySpan<byte> remainingData = data;
            while (remainingData.Length >= BytesPerLine)
            {
                charsWritten += WriteBase64(remainingData.Slice(0, BytesPerLine), destination, charsWritten);
                charsWritten += Write(NewLine, destination, charsWritten);
                remainingData = remainingData.Slice(BytesPerLine);
            }

            Debug.Assert(remainingData.Length < BytesPerLine);

            if (remainingData.Length > 0)
            {
                charsWritten += WriteBase64(remainingData, destination, charsWritten);
                charsWritten += Write(NewLine, destination, charsWritten);
                remainingData = default;
            }

            charsWritten += Write(PostEBPrefix, destination, charsWritten);
            charsWritten += Write(label, destination, charsWritten);
            charsWritten += Write(Ending, destination, charsWritten);

            return true;
        }

        /// <summary>
        /// Creates an encoded PEM with the given label and data.
        /// </summary>
        /// <param name="label">
        /// The label to encode.
        /// </param>
        /// <param name="data">
        /// The data to encode.
        /// </param>
        /// <returns>
        /// A character array of the encoded PEM.
        /// </returns>
        /// <remarks>
        /// This method always wraps the base-64 encoded text to 64 characters, per the
        /// recommended wrapping of RFC-7468. Unix-style line endings are used for line breaks.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="label"/> exceeds the maximum possible label length.
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <paramref name="data"/> exceeds the maximum possible encoded data length.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The resulting PEM-encoded text is larger than <see cref="int.MaxValue"/>.
        ///   <para>
        ///       - or -
        ///   </para>
        /// <paramref name="label"/> contains invalid characters.
        /// </exception>
        public static char[] Write(ReadOnlySpan<char> label, ReadOnlySpan<byte> data)
        {

            int encodedSize = GetEncodedSize(label.Length, data.Length);
            char[] buffer = new char[encodedSize];

            if (!TryWrite(label, data, buffer, out int charsWritten))
            {
                Debug.Fail("TryWrite failed with a pre-sized buffer");
                throw new ArgumentException(null, nameof(data));
            }

            Debug.Assert(charsWritten == encodedSize);
            return buffer;
        }
    }
}
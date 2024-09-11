using System;
using System.Text;

namespace Microsoft.FeatureManagement.Extensions
{
    internal static class BytesExtensions
    {
        /// <summary>
        /// Converts a byte array to Base64URL string with optional padding ('=') characters removed.
        /// Base64 description: https://datatracker.ietf.org/doc/html/rfc4648.html#section-4
        /// </summary>
        public static string ToBase64Url(this byte[] bytes)
        {
            string bytesBase64 = Convert.ToBase64String(bytes);

            int indexOfEquals = bytesBase64.IndexOf("=");

            // Skip the optional padding of '=' characters based on the Base64Url spec if any are present from the Base64 conversion
            int stringBuilderCapacity = indexOfEquals != -1 ? indexOfEquals : bytesBase64.Length;

            StringBuilder stringBuilder = new StringBuilder(stringBuilderCapacity);

            // Construct Base64URL string by replacing characters in Base64 conversion that are not URL safe
            for (int i = 0; i < stringBuilderCapacity; i++)
            {
                if (bytesBase64[i] == '+')
                {
                    stringBuilder.Append('-');
                }
                else if (bytesBase64[i] == '/')
                {
                    stringBuilder.Append('_');
                }
                else
                {
                    stringBuilder.Append(bytesBase64[i]);
                }
            }

            return stringBuilder.ToString();
        }
    }
}

using System;
using System.Buffers;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Common.Serializer
{
    public class JsonBigIntegerConverter: JsonConverter<BigInteger>
    {
        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (!TryGetBigInteger(ref reader, out var result))
                result = BigInteger.Zero;
            return result;
        }

        public override void Write(Utf8JsonWriter writer, BigInteger versionValue, JsonSerializerOptions options) =>
            writer.WriteRawValue(versionValue.ToString());

        private static bool TryGetBigInteger(ref Utf8JsonReader reader, out BigInteger bi)
        {
            var byteArray = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray();
            var str = Encoding.UTF8.GetString(byteArray);
            return BigInteger.TryParse(str, out bi);
        }
    }
}

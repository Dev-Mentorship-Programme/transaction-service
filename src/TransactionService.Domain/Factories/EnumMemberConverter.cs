using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace TransactionService.Domain.Factories
{
    public class EnumMemberConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly Dictionary<string, T> _enumValues;
        private readonly Dictionary<T, string> _enumNames;

        public EnumMemberConverter()
        {
            _enumValues = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            _enumNames = [];

            foreach (T value in Enum.GetValues<T>())
            {
                var field = typeof(T).GetField(value.ToString());
                var enumMemberAttr = field?.GetCustomAttribute<EnumMemberAttribute>();
                
                if (enumMemberAttr?.Value != null)
                {
                    _enumValues[enumMemberAttr.Value] = value;
                    _enumNames[value] = enumMemberAttr.Value;
                }
                else
                {
                    _enumValues[value.ToString()] = value;
                    _enumNames[value] = value.ToString();
                }
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (stringValue != null && _enumValues.TryGetValue(stringValue, out var enumValue))
                {
                    return enumValue;
                }
                throw new JsonException($"Unable to convert \"{stringValue}\" to enum {typeof(T).Name}");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                var intValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(T), intValue))
                {
                    return (T)Enum.ToObject(typeof(T), intValue);
                }
            }

            throw new JsonException($"Unable to convert token type {reader.TokenType} to enum {typeof(T).Name}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (_enumNames.TryGetValue(value, out var name))
            {
                writer.WriteStringValue(name);
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
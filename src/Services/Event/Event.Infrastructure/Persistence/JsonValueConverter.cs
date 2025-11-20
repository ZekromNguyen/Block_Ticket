using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Event.Infrastructure.Persistence;

public class JsonValueConverter<T> : ValueConverter<T, string>
{
    public JsonValueConverter(ConverterMappingHints? mappingHints = null)
        : base(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null),
            mappingHints)
    {
    }
}


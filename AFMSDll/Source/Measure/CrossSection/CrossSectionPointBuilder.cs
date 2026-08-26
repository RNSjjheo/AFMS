using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AFMSDll
{
    public static class CrossSectionPointBuilder
    {
        public const string JSON_NODE_DIST = "Dist";
        public const string JSON_NODE_ELEV = "Elev";

        private const string JSON_NODE_POINTS = "Points";
        private const string JSON_NODE_WATER_LEVEL = "WaterLevel";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public static CrossSectionPointCollection Build(IEnumerable<CrossSectionPoint> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            CrossSectionPointCollection result = new();
            result.AddRange(source);
            return result;
        }

        public static CrossSectionPointCollection Build<T>(IEnumerable<T> source, Func<T, CrossSectionPoint> converter)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(converter);

            CrossSectionPointCollection result = new();

            foreach (T item in source)
            {
                CrossSectionPoint point = converter(item) ?? throw new InvalidOperationException("CrossSectionPoint 변환 결과가 null입니다.");
                result.Add(point);
            }

            return result;
        }

        public static CrossSectionPointCollection Build(string json, double zeroPointElevation = 0.0)
        {
            if (string.IsNullOrWhiteSpace(json)) return new CrossSectionPointCollection();

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonElement pointElements;
            double? waterLevel = null;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty(JSON_NODE_POINTS, out JsonElement points) &&
                points.ValueKind == JsonValueKind.Array)
            {
                pointElements = points;

                if (root.TryGetProperty(JSON_NODE_WATER_LEVEL, out JsonElement level) &&
                    level.ValueKind == JsonValueKind.Number && level.TryGetDouble(out double value))
                {
                    waterLevel = value;
                }
            }
            else
            {
                throw new JsonException("단면 좌표 배열을 찾을 수 없습니다.");
            }

            CrossSectionPointCollection result = new CrossSectionPointCollection
            {
                WaterLevel = waterLevel
            };

            foreach (JsonElement element in pointElements.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                    throw new JsonException("단면 좌표 형식이 올바르지 않습니다.");

                double leftBankDistance = ReadCoordinate(element, JSON_NODE_DIST);
                double elevation = ReadCoordinate(element, JSON_NODE_ELEV) - zeroPointElevation;

                result.Add(new CrossSectionPoint(leftBankDistance, elevation));
            }

            return result;
        }

        public static string GetJson(CrossSectionPointCollection source)
        {
            ArgumentNullException.ThrowIfNull(source);

            CrossSectionPointDataJson data = new CrossSectionPointDataJson
            {
                WaterLevel = source.WaterLevel,
                Points = source.ConvertAll(point => new CrossSectionPointJson
                {
                    LeftBankDistance = point.LeftBankDistance,
                    Elevation = point.Elevation
                })
            };

            return JsonSerializer.Serialize(data, JsonOptions);
        }

        private static double ReadCoordinate(JsonElement element, string propertyName)
        {
            if (TryReadFiniteDouble(element, propertyName, out double value)) return value;

            throw new JsonException($"단면 좌표의 {propertyName} 값이 없거나 올바르지 않습니다.");
        }

        private static bool TryReadFiniteDouble(JsonElement element, string propertyName, out double value)
        {
            value = 0.0;
            return element.TryGetProperty(propertyName, out JsonElement property) &&
                   property.ValueKind == JsonValueKind.Number &&
                   property.TryGetDouble(out value) && double.IsFinite(value);
        }

        private sealed class CrossSectionPointDataJson
        {
            public double? WaterLevel { get; set; }
            public List<CrossSectionPointJson> Points { get; set; } = new List<CrossSectionPointJson>();
        }

        private sealed class CrossSectionPointJson
        {
            [JsonPropertyName(JSON_NODE_DIST)]
            public double LeftBankDistance { get; set; }

            [JsonPropertyName(JSON_NODE_ELEV)]
            public double Elevation { get; set; }
        }
    }
}

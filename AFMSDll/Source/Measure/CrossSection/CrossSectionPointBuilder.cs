using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AFMSDll
{
    public static class CrossSectionPointBuilder
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = true
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

            if (root.ValueKind == JsonValueKind.Array)
            {
                pointElements = root;
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     root.TryGetProperty("Points", out JsonElement points) &&
                     points.ValueKind == JsonValueKind.Array)
            {
                pointElements = points;

                if (root.TryGetProperty("WaterLevel", out JsonElement level) &&
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

                double x = ReadCoordinate(element, "X", "Dist");
                double y = ReadCoordinate(element, "Y", "Elev") - zeroPointElevation;

                result.Add(new CrossSectionPoint(x, y));
            }

            return result;
        }

        public static string GetJson(CrossSectionPointCollection source)
        {
            ArgumentNullException.ThrowIfNull(source);

            CrossSectionPointDataJson data = new CrossSectionPointDataJson
            {
                WaterLevel = source.WaterLevel,
                Points = source
            };

            return JsonSerializer.Serialize(data, JsonOptions);
        }

        private static double ReadCoordinate(JsonElement element, string currentName, string legacyName)
        {
            if (TryReadFiniteDouble(element, currentName, out double value) ||
                TryReadFiniteDouble(element, legacyName, out value)) return value;

            throw new JsonException($"단면 좌표의 {currentName} 값이 없거나 올바르지 않습니다.");
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
            public CrossSectionPointCollection Points { get; set; } = new CrossSectionPointCollection();
        }
    }
}

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution
{
    internal static class ObservabilityTraceFormatter
    {
        public static string BuildTraceId(params string[] parts)
        {
            string seed = BuildSeed(parts);
            if (string.IsNullOrWhiteSpace(seed))
            {
                return "trace-0000000000000000";
            }

            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(seed));
                return $"trace-{ToHexLower(hash, 8)}";
            }
        }

        public static string BuildCycleHash(string cycleSignature)
        {
            return BuildTraceId(cycleSignature);
        }

        public static string BuildCycleTraceId(string cycleSignature)
        {
            return BuildTraceId(cycleSignature);
        }

        public static string BuildCompactLogMessage(string headline, params (string Key, object Value)[] fields)
        {
            var builder = new StringBuilder(headline.TrimToEmpty());
            if (fields == null || fields.Length == 0)
            {
                return builder.ToString();
            }

            for (int index = 0; index < fields.Length; index += 1)
            {
                (string key, object value) = fields[index];
                if (string.IsNullOrWhiteSpace(key) || value == null)
                {
                    continue;
                }

                string text = FormatValue(value);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                builder.Append(' ');
                builder.Append(key.Trim());
                builder.Append("='");
                builder.Append(text);
                builder.Append('\'');
            }

            return builder.ToString();
        }

        private static string BuildSeed(params string[] parts)
        {
            if (parts == null || parts.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int index = 0; index < parts.Length; index += 1)
            {
                if (index > 0)
                {
                    builder.Append('|');
                }

                builder.Append(parts[index].TrimToEmpty());
            }

            return builder.ToString();
        }

        private static string FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string text:
                    return text.TrimToEmpty();
                case bool boolean:
                    return boolean ? "true" : "false";
                case Enum enumeration:
                    return enumeration.ToString();
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }
        private static string ToHexLower(byte[] bytes, int length)
        {
            if (bytes == null || bytes.Length == 0 || length <= 0)
            {
                return string.Empty;
            }

            int byteCount = Math.Min(bytes.Length, length);
            var builder = new StringBuilder(byteCount * 2);
            for (int index = 0; index < byteCount; index += 1)
            {
                builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }
}

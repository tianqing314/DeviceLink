using System;
using System.Globalization;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// 响应文本的数值解析 —— L6 表示层的公共工具。
    ///
    /// 存在意义：ConST860 / ConST685 / ConST171A 三个设备曾各自复制了一份
    /// 字节级相同的 <c>ParseDouble</c> / <c>ParseInt</c>（InvariantCulture + Trim + NaN 兜底）。
    /// 数值格式解释属于表示层职责，必须只有一份实现，否则"小数点用逗号"这类
    /// 区域性差异会在不同设备上给出不一致结果。
    /// </summary>
    public static class NumericText
    {
        /// <summary>解析为双精度浮点；失败返回 <paramref name="fallback"/>（默认 NaN，便于连续测量场景）。</summary>
        /// <param name="text">设备响应文本</param>
        /// <param name="fallback">解析失败时的返回值</param>
        public static double ParseDouble(string? text, double fallback = double.NaN)
            => double.TryParse((text ?? string.Empty).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        /// <summary>解析为整数；失败返回 <paramref name="fallback"/>（默认 0）。</summary>
        /// <param name="text">设备响应文本</param>
        /// <param name="fallback">解析失败时的返回值</param>
        public static int ParseInt(string? text, int fallback = 0)
            => int.TryParse((text ?? string.Empty).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        /// <summary>解析为长整数；失败返回 <paramref name="fallback"/>（默认 0）。</summary>
        /// <param name="text">设备响应文本</param>
        /// <param name="fallback">解析失败时的返回值</param>
        public static long ParseLong(string? text, long fallback = 0L)
            => long.TryParse((text ?? string.Empty).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;

        /// <summary>尝试解析双精度浮点。</summary>
        /// <param name="text">设备响应文本</param>
        /// <param name="value">解析结果</param>
        public static bool TryParseDouble(string? text, out double value)
            => double.TryParse((text ?? string.Empty).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out value);

        /// <summary>尝试解析整数。</summary>
        /// <param name="text">设备响应文本</param>
        /// <param name="value">解析结果</param>
        public static bool TryParseInt(string? text, out int value)
            => int.TryParse((text ?? string.Empty).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out value);

        /// <summary>按分隔符拆分并逐项解析为双精度浮点，解析失败的项为 <see cref="double.NaN"/>。</summary>
        /// <param name="text">设备响应文本</param>
        /// <param name="separator">分隔符</param>
        public static double[] ParseDoubles(string? text, char separator = ',')
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<double>();

            var parts = text!.Split(separator);
            var result = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                result[i] = ParseDouble(parts[i]);

            return result;
        }

        /// <summary>把文本按 InvariantCulture 格式化为设备可接受的数字字符串。</summary>
        /// <param name="value">数值</param>
        /// <param name="format">格式串</param>
        public static string Format(double value, string format = "G")
            => value.ToString(format, CultureInfo.InvariantCulture);
    }
}

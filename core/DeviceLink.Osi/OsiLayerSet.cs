using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceLink.Osi
{
    /// <summary>
    /// OSI 层集合 —— 不可变值类型，支持集合运算与工程习惯的字符串表示。
    ///
    /// 显示规则与工程文档一致：连续 3 层及以上压缩为区间，否则逐层列出。
    /// 例如：
    ///   L1|L2|L7        → "L1,L2,L7"        （RS-232 串口栈）
    ///   L1|L2|L3|L4|L5|L7 → "L1-L5,L7"      （MQTT 栈）
    ///   L1|L2|L3|L4|L7  → "L1-L4,L7"        （Modbus TCP 栈）
    /// </summary>
    public readonly struct OsiLayerSet : IEquatable<OsiLayerSet>
    {
        private readonly OsiLayer _layers;

        /// <summary>构造层集合，超出七层范围的位会被自动裁掉。</summary>
        public OsiLayerSet(OsiLayer layers)
        {
            _layers = layers & OsiLayerInfo.AllLayers;
        }

        /// <summary>空集合。</summary>
        public static OsiLayerSet Empty => new OsiLayerSet(OsiLayer.None);

        /// <summary>七层全集。</summary>
        public static OsiLayerSet All => new OsiLayerSet(OsiLayerInfo.AllLayers);

        /// <summary>底层原始位值。</summary>
        public OsiLayer Layers => _layers;

        /// <summary>是否为空。</summary>
        public bool IsEmpty => _layers == OsiLayer.None;

        /// <summary>是否为七层全集。</summary>
        public bool IsAll => _layers == OsiLayerInfo.AllLayers;

        /// <summary>包含的层数量。</summary>
        public int Count
        {
            get
            {
                int c = 0;
                for (int i = 0; i < 7; i++)
                {
                    if ((_layers & OsiLayerInfo.At(i)) != 0) c++;
                }
                return c;
            }
        }

        /// <summary>是否包含指定层（参数为单层或层组合，组合时要求全部包含）。</summary>
        public bool Contains(OsiLayer layer)
        {
            if (layer == OsiLayer.None) return false;
            return (_layers & layer) == layer;
        }

        /// <summary>是否包含另一层集合的全部层。</summary>
        public bool Contains(OsiLayerSet other)
        {
            return (other._layers & _layers) == other._layers;
        }

        /// <summary>并集。</summary>
        public OsiLayerSet Union(OsiLayerSet other) => new OsiLayerSet(_layers | other._layers);

        /// <summary>交集。</summary>
        public OsiLayerSet Intersect(OsiLayerSet other) => new OsiLayerSet(_layers & other._layers);

        /// <summary>差集（本集合中有、other 中没有的层）。</summary>
        public OsiLayerSet Except(OsiLayerSet other) => new OsiLayerSet(_layers & ~other._layers);

        /// <summary>构造包含指定若干层的集合。</summary>
        public static OsiLayerSet Of(params OsiLayer[] layers)
        {
            OsiLayer acc = OsiLayer.None;
            if (layers != null)
            {
                foreach (var l in layers) acc |= l;
            }
            return new OsiLayerSet(acc);
        }

        /// <summary>构造从 from 到 to 的连续层区间（含两端）。</summary>
        public static OsiLayerSet Range(OsiLayer from, OsiLayer to)
        {
            int a = OsiLayerInfo.NumberOf(from);
            int b = OsiLayerInfo.NumberOf(to);
            if (a == 0 || b == 0)
                throw new ArgumentException("Range 的端点必须是单层");
            if (a > b)
            {
                int t = a; a = b; b = t;
            }

            OsiLayer acc = OsiLayer.None;
            for (int n = a; n <= b; n++) acc |= OsiLayerInfo.At(n - 1);
            return new OsiLayerSet(acc);
        }

        /// <summary>
        /// 解析形如 "L1-L4,L5,L7" / "1-4,5,7" / "L1,L2,L7" 的层表达式。
        /// 大小写无关，忽略空白。
        /// </summary>
        public static OsiLayerSet Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("层表达式不能为空", nameof(text));

            OsiLayer acc = OsiLayer.None;
            var segments = text.Split(new[] { ',', '，', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in segments)
            {
                var seg = raw.Trim();
                if (seg.Length == 0) continue;

                int dash = seg.IndexOfAny(new[] { '-', '~' });
                if (dash > 0)
                {
                    var lo = ParseSingle(seg.Substring(0, dash));
                    var hi = ParseSingle(seg.Substring(dash + 1));
                    acc |= Range(lo, hi).Layers;
                }
                else
                {
                    acc |= ParseSingle(seg);
                }
            }

            return new OsiLayerSet(acc);
        }

        private static OsiLayer ParseSingle(string token)
        {
            var t = token.Trim();
            if (t.StartsWith("L", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);

            if (!int.TryParse(t, out int n) || n < 1 || n > 7)
                throw new FormatException($"无法解析 OSI 层标记: '{token}'，合法值为 L1~L7");

            return OsiLayerInfo.At(n - 1);
        }

        /// <summary>转换为 "L1-L4,L5,L7" 形式。</summary>
        public override string ToString()
        {
            if (IsEmpty) return "∅";

            var sb = new StringBuilder();
            int runStart = -1;
            int runLength = 0;

            for (int i = 0; i < 7; i++)
            {
                bool has = (_layers & OsiLayerInfo.At(i)) != 0;
                if (has)
                {
                    if (runStart < 0) runStart = i;
                    runLength++;
                }
                else
                {
                    AppendRun(sb, runStart, runLength);
                    runStart = -1;
                    runLength = 0;
                }
            }
            AppendRun(sb, runStart, runLength);
            return sb.ToString();
        }

        private static void AppendRun(StringBuilder sb, int start, int length)
        {
            if (length <= 0) return;
            if (sb.Length > 0) sb.Append(',');

            if (length >= 3)
            {
                sb.Append('L').Append(start + 1).Append('-').Append('L').Append(start + length);
            }
            else
            {
                for (int k = 0; k < length; k++)
                {
                    if (k > 0) sb.Append(',');
                    sb.Append('L').Append(start + k + 1);
                }
            }
        }

        /// <inheritdoc/>
        public bool Equals(OsiLayerSet other) => _layers == other._layers;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is OsiLayerSet other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => (int)_layers;

        /// <summary>相等运算符。</summary>
        public static bool operator ==(OsiLayerSet left, OsiLayerSet right) => left.Equals(right);

        /// <summary>不等运算符。</summary>
        public static bool operator !=(OsiLayerSet left, OsiLayerSet right) => !left.Equals(right);

        /// <summary>并集运算符。</summary>
        public static OsiLayerSet operator |(OsiLayerSet left, OsiLayerSet right) => left.Union(right);

        /// <summary>交集运算符。</summary>
        public static OsiLayerSet operator &(OsiLayerSet left, OsiLayerSet right) => left.Intersect(right);

        /// <summary>差集运算符。</summary>
        public static OsiLayerSet operator -(OsiLayerSet left, OsiLayerSet right) => left.Except(right);
    }
}

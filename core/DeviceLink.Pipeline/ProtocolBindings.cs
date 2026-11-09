using System;
using System.Collections.Generic;
using System.Text;
using DeviceLink.DataLink;
using DeviceLink.Osi;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;

namespace DeviceLink.Pipeline
{
    /// <summary>
    /// 协议 → 帧策略 / 协议栈画像 的绑定表。
    ///
    /// 解决的问题：过去"某协议该用哪种帧"由 <c>DeviceBase</c> 里的 <c>switch (codec)</c> 决定，
    /// 后果是
    /// · 设备基类同时依赖 Protocol 与 DataLink 的具体类型（层次倒置）；
    /// · 新增协议时必须改基类，且漏改会静默退化为分隔符帧（Modbus TCP 就踩了这个坑）。
    ///
    /// 现在集中在此登记，并且：
    /// · <b>未登记的协议不会被猜成"分隔符帧"</b>，而是显式抛出，避免静默错误；
    /// · 帧策略的已知依赖（例如 MBAP 长度字段偏移）由协议自身以常量暴露，不在这里硬编码。
    ///
    /// 扩展方式：<see cref="Register"/> 注册自定义协议，无需修改任何核心类。
    /// </summary>
    public static class ProtocolBindings
    {
        /// <summary>协议绑定条目。</summary>
        public sealed class Binding
        {
            internal Binding(
                Func<IProtocolCodec, IPhysicalTransport?, IFrameStrategy> frameStrategy,
                Func<IPhysicalTransport?, OsiStackProfile> profile)
            {
                FrameStrategy = frameStrategy;
                Profile = profile;
            }

            /// <summary>按协议实例与传输介质创建帧策略。</summary>
            public Func<IProtocolCodec, IPhysicalTransport?, IFrameStrategy> FrameStrategy { get; }

            /// <summary>按传输介质选择协议栈画像。</summary>
            public Func<IPhysicalTransport?, OsiStackProfile> Profile { get; }
        }

        private static readonly Dictionary<Type, Binding> _bindings = new();
        private static readonly object _sync = new();

        static ProtocolBindings()
        {
            RegisterDefaults();
        }

        /// <summary>登记一个协议的帧策略与画像。重复登记会覆盖旧值。</summary>
        /// <param name="codecType">协议编解码器类型</param>
        /// <param name="frameStrategy">帧策略工厂，参数为底层传输（可能为 null，表示无传输的会话型协议）</param>
        /// <param name="profile">画像工厂，参数为底层传输</param>
        public static void Register(
            Type codecType,
            Func<IProtocolCodec, IPhysicalTransport?, IFrameStrategy> frameStrategy,
            Func<IPhysicalTransport?, OsiStackProfile> profile)
        {
            if (codecType == null) throw new ArgumentNullException(nameof(codecType));
            if (frameStrategy == null) throw new ArgumentNullException(nameof(frameStrategy));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            lock (_sync)
            {
                _bindings[codecType] = new Binding(frameStrategy, profile);
            }
        }

        /// <summary>该协议是否已登记。</summary>
        public static bool IsRegistered(IProtocolCodec codec)
        {
            if (codec == null) return false;
            lock (_sync)
            {
                return _bindings.ContainsKey(codec.GetType());
            }
        }

        /// <summary>
        /// 取该协议在指定传输上的推荐帧策略。未登记的协议返回 null（由调用方决定回退策略）。
        /// </summary>
        public static IFrameStrategy? TryCreateFrameStrategy(IProtocolCodec codec, IPhysicalTransport? transport)
        {
            var binding = Find(codec);
            return binding?.FrameStrategy(codec, transport);
        }

        /// <summary>
        /// 取该协议在指定传输上的协议栈画像。未登记返回 null（此时跳过 OSI 校验并告警）。
        /// </summary>
        public static OsiStackProfile? TryResolveProfile(
            IProtocolCodec codec, IPhysicalTransport? transport, ISession? session = null)
        {
            if (session is MqttSession) return OsiProfiles.Mqtt;

            var binding = Find(codec);
            return binding?.Profile(transport);
        }

        private static Binding? Find(IProtocolCodec codec)
        {
            if (codec == null) return null;

            lock (_sync)
            {
                if (_bindings.TryGetValue(codec.GetType(), out var exact))
                    return exact;

                // 允许通过基类/接口登记（自定义协议继承内置编解码器时的兜底）
                foreach (var pair in _bindings)
                {
                    if (pair.Key.IsInstanceOfType(codec))
                        return pair.Value;
                }
            }

            return null;
        }

        // ───────────────────────── 传输介质判别 ─────────────────────────

        /// <summary>是否 IP 网络传输（TCP/UDP）。</summary>
        public static bool IsIpTransport(IPhysicalTransport? transport)
            => transport is TcpTransport || transport is UdpTransport;

        /// <summary>是否蓝牙 SPP 传输。</summary>
        public static bool IsBluetoothTransport(IPhysicalTransport? transport)
            => transport is BluetoothTransport;

        /// <summary>是否 Zigbee 透传模块传输。</summary>
        public static bool IsZigbeeTransport(IPhysicalTransport? transport)
            => transport is ZigbeeTransport;

        // ───────────────────────── 默认登记 ─────────────────────────

        private static void RegisterDefaults()
        {
            // Modbus TCP —— MBAP 是长度前缀，必须用 LengthPrefixedFrameStrategy。
            // 用分隔符（\0）定帧会在载荷含 0x00 时错帧，这是历史上的真实缺陷。
            Register(
                typeof(ModbusTcpCodec),
                (_, __) => new LengthPrefixedFrameStrategy(
                    lengthFieldOffset: ModbusTcpCodec.LengthFieldOffset),
                _ => OsiProfiles.ModbusTcp);

            // Modbus RTU —— CRC16 + 按功能码定长
            Register(
                typeof(ModbusRtuCodec),
                (_, __) => new ModbusRtuFrameStrategy(),
                t => IsIpTransport(t) ? OsiProfiles.ModbusRtuOverTcp : OsiProfiles.ModbusRtu);

            // ZQWL 私有帧
            Register(
                typeof(ZqwlCodec),
                (_, __) => new ZqwlFrameStrategy(),
                _ => OsiProfiles.Rs232);

            // SCPI —— 用编解码器自己的终止符（可能是 \n 或 \r\n，取决于构造参数）
            Register(
                typeof(ScpiCodec),
                (c, _) => FromTerminator(c),
                t => SelectTextProfile(t, OsiProfiles.ScpiOverTcp, OsiProfiles.ScpiOverRs232));

            // ConST —— 用编解码器自己的终止符
            Register(
                typeof(ConSTCodec),
                (c, _) => FromTerminator(c),
                t => SelectTextProfile(t, OsiProfiles.ConSTOverTcp, OsiProfiles.ConSTOverRs232));
        }

        /// <summary>按协议自带的帧终止符创建分隔符帧策略。</summary>
        private static IFrameStrategy FromTerminator(IProtocolCodec codec)
        {
            if (codec is IFramedProtocol framed && framed.FrameTerminator.Length > 0)
                return new DelimiterFrameStrategy(framed.FrameTerminator);

            throw new InvalidOperationException(
                $"协议 {codec.ProtocolName} 未提供帧终止符，无法推导分隔符帧策略。");
        }

        private static OsiStackProfile SelectTextProfile(
            IPhysicalTransport? transport,
            OsiStackProfile ipProfile,
            OsiStackProfile serialProfile)
        {
            if (IsBluetoothTransport(transport)) return OsiProfiles.BluetoothSpp;
            if (IsZigbeeTransport(transport)) return OsiProfiles.ZigbeeTransparent;
            if (IsIpTransport(transport)) return ipProfile;
            return serialProfile;
        }
    }
}

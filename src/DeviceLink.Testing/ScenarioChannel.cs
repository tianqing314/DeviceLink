using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Core.Channel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceLink.Testing
{
    /// <summary>
    /// 场景驱动的测试通道 —— 根据预设的请求→响应规则模拟设备行为。
    /// 
    /// 使用示例：
    ///   var scenario = new ChannelScenario();
    ///   scenario.When(req => Encoding.ASCII.GetString(req).Contains("PRES"))
    ///           .Respond(Encoding.ASCII.GetBytes("1:F:PRES:1.23456\0"));
    ///   scenario.When(req => Encoding.ASCII.GetString(req).Contains("PRZ"))
    ///           .Respond(Encoding.ASCII.GetBytes("1:F:PRZ:\0"));
    ///   
    ///   var channel = new ScenarioChannel(scenario);
    ///   var dpsex = new DPSEX(channel);
    ///   var pressure = await dpsex.GetPressureAsync(); // → 1.23456
    /// </summary>
    public class ScenarioChannel : IChannel
    {
        private readonly ChannelScenario _scenario;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isOpen = true;
        private bool _disposed;

        public ScenarioChannel(ChannelScenario scenario, ILogger? logger = null)
        {
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            _logger = logger ?? NullLogger.Instance;
        }

        public string Name { get; set; } = "ScenarioChannel";
        public bool IsOpen => _isOpen;

        public Task OpenAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task CloseAsync() { _isOpen = false; return Task.CompletedTask; }

        public async Task<ChannelResult> SendAndReceiveAsync(
            byte[] request,
            ChannelPolicy? policy = null,
            CancellationToken ct = default)
        {
            if (request == null || request.Length == 0)
                return ChannelResult.Fail("请求数据为空");

            policy ??= ChannelPolicy.Default;

            await _semaphore.WaitAsync(ct);
            try
            {
                var sw = Stopwatch.StartNew();

                var rule = _scenario.Match(request);
                if (rule == null)
                {
                    _logger.LogWarning("[{Channel}] 未匹配到场景规则", Name);
                    return ChannelResult.Fail("未匹配到场景规则");
                }

                // 模拟延迟
                if (rule.ResponseDelayMs > 0)
                    await Task.Delay(rule.ResponseDelayMs, ct);

                sw.Stop();

                if (rule.ShouldTimeout)
                    return ChannelResult.Timeout();

                if (rule.ErrorMessage != null)
                    return ChannelResult.Fail(rule.ErrorMessage);

                _logger.LogDebug("[{Channel}] 场景匹配: {Rule} → 响应 {Bytes} bytes",
                    Name, rule.Name, rule.Response?.Length ?? 0);

                return ChannelResult.Succeed(
                    rule.Response ?? Array.Empty<byte>(),
                    elapsed: sw.Elapsed);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SendOnlyAsync(byte[] request, CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                _logger.LogDebug("[{Channel}] 单向发送 ({Bytes} bytes)", Name, request.Length);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<ChannelResult> ReceiveOnlyAsync(
            ChannelPolicy? policy = null,
            CancellationToken ct = default)
        {
            return Task.FromResult(ChannelResult.Fail("ScenarioChannel 不支持纯接收"));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _semaphore.Dispose();
        }
    }

    /// <summary>
    /// 通道交互场景 —— 定义一组请求→响应规则
    /// </summary>
    public class ChannelScenario
    {
        private readonly List<ScenarioRule> _rules = new();

        /// <summary>
        /// 定义一个匹配规则
        /// </summary>
        public ScenarioRuleBuilder When(Func<byte[], bool> predicate)
        {
            return new ScenarioRuleBuilder(this, predicate);
        }

        /// <summary>
        /// 添加一条规则
        /// </summary>
        public void AddRule(ScenarioRule rule)
        {
            _rules.Add(rule);
        }

        /// <summary>
        /// 匹配第一个符合条件的规则，无匹配返回 null
        /// </summary>
        public ScenarioRule? Match(byte[] request)
        {
            return _rules.FirstOrDefault(r => r.Predicate(request));
        }
    }

    /// <summary>
    /// 场景规则
    /// </summary>
    public class ScenarioRule
    {
        public string Name { get; set; } = "unnamed";
        public Func<byte[], bool> Predicate { get; set; } = _ => true;
        public byte[]? Response { get; set; }
        public int ResponseDelayMs { get; set; } = 0;
        public bool ShouldTimeout { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 流式规则构建器
    /// </summary>
    public class ScenarioRuleBuilder
    {
        private readonly ChannelScenario _scenario;
        private readonly ScenarioRule _rule = new();

        internal ScenarioRuleBuilder(ChannelScenario scenario, Func<byte[], bool> predicate)
        {
            _scenario = scenario;
            _rule.Predicate = predicate;
        }

        /// <summary>设定匹配成功时返回的字节数据</summary>
        public ScenarioRuleBuilder Respond(byte[] response)
        {
            _rule.Response = response;
            return this;
        }

        /// <summary>设定模拟延迟（毫秒）</summary>
        public ScenarioRuleBuilder Delay(int milliseconds)
        {
            _rule.ResponseDelayMs = milliseconds;
            return this;
        }

        /// <summary>设定此规则触发超时</summary>
        public ScenarioRuleBuilder Timeout()
        {
            _rule.ShouldTimeout = true;
            return this;
        }

        /// <summary>设定此规则返回错误</summary>
        public ScenarioRuleBuilder FailWith(string errorMessage)
        {
            _rule.ErrorMessage = errorMessage;
            return this;
        }

        /// <summary>设定规则名称（用于调试日志）</summary>
        public ScenarioRuleBuilder Named(string name)
        {
            _rule.Name = name;
            return this;
        }

        /// <summary>完成规则定义，注册到场景中</summary>
        public ChannelScenario Add()
        {
            _scenario.AddRule(_rule);
            return _scenario;
        }
    }
}

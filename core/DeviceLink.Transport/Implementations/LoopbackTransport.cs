using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 内存回环传输 —— 在内存中模拟设备收发，不依赖任何物理硬件。
    /// 
    /// 使用方式：
    ///   1. 创建 LoopbackTransport
    ///   2. 设置 OnSend 回调，在回调中调用 EnqueueReceive 模拟设备回复
    ///   3. 传给会话层使用
    /// </summary>
    public class LoopbackTransport : IPhysicalTransport
    {
        private readonly Queue<byte[]> _receiveQueue = new();
        private readonly List<byte> _currentReceive = new();
        private readonly object _lock = new();
        private bool _isOpen;
        private readonly ILogger<LoopbackTransport>? _logger;

        /// <summary>
        /// 初始化回环传输
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public LoopbackTransport(ILogger<LoopbackTransport>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>发送时触发，参数为发送的字节数据</summary>
        public event Action<byte[]>? OnSend;

        /// <inheritdoc/>
        public string Name { get; set; } = "Loopback";

        /// <inheritdoc/>
        public bool IsOpen => _isOpen;

        /// <inheritdoc/>
        public Task ConnectAsync(CancellationToken ct = default)
        {
            _isOpen = true;
            _logger?.LogInformation("回环传输已连接");
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            _isOpen = false;
            _logger?.LogInformation("回环传输已关闭");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 将字节数据推入接收队列，模拟设备主动发送数据。
        /// 在 OnSend 回调中调用此方法可以实现"收到命令 → 返回响应"的模拟。
        /// </summary>
        /// <param name="data">要入队的数据</param>
        public void EnqueueReceive(byte[] data)
        {
            lock (_lock)
            {
                _receiveQueue.Enqueue(data);
                _logger?.LogDebug("向回环传输入队了 {Count} 字节数据", data.Length);
            }
        }

        /// <inheritdoc/>
        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            lock (_lock)
            {
                // 先从队列取出待消费的数据
                if (_currentReceive.Count == 0 && _receiveQueue.Count > 0)
                {
                    _currentReceive.AddRange(_receiveQueue.Dequeue());
                }

                if (_currentReceive.Count == 0)
                    return Task.FromResult(0);

                int toRead = Math.Min(count, _currentReceive.Count);
                Array.Copy(_currentReceive.ToArray(), 0, buffer, offset, toRead);
                _currentReceive.RemoveRange(0, toRead);
                _logger?.LogDebug("从回环传输读取了 {Count} 字节", toRead);
                return Task.FromResult(toRead);
            }
        }

        /// <inheritdoc/>
        public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            var sent = new byte[count];
            Array.Copy(data, offset, sent, 0, count);

            // 触发回调，让测试代码有机会 EnqueueReceive
            OnSend?.Invoke(sent);
            _logger?.LogDebug("向回环传输写入了 {Count} 字节", count);

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                _receiveQueue.Clear();
                _currentReceive.Clear();
                _logger?.LogDebug("已清空回环传输接收缓冲区");
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _isOpen = false;
            _receiveQueue.Clear();
            _currentReceive.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Core.Transport;

namespace DeviceLink.Testing
{
    /// <summary>
    /// 内存回环传输 —— 在内存中模拟设备收发，不依赖任何物理硬件。
    /// 
    /// 使用方式：
    ///   1. 创建 LoopbackTransport
    ///   2. 设置 OnSend 回调，在回调中调用 EnqueueReceive 模拟设备回复
    ///   3. 传给 DirectChannel 使用
    /// </summary>
    public class LoopbackTransport : IByteTransport
    {
        private readonly Queue<byte[]> _receiveQueue = new();
        private readonly List<byte> _currentReceive = new();
        private readonly object _lock = new();
        private bool _isOpen;

        /// <summary>发送时触发，参数为发送的字节数据</summary>
        public event Action<byte[]>? OnSend;

        public string Name { get; set; } = "Loopback";

        public bool IsOpen => _isOpen;

        public Task ConnectAsync(CancellationToken ct = default)
        {
            _isOpen = true;
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            _isOpen = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 将字节数据推入接收队列，模拟设备主动发送数据。
        /// 在 OnSend 回调中调用此方法可以实现"收到命令 → 返回响应"的模拟。
        /// </summary>
        public void EnqueueReceive(byte[] data)
        {
            lock (_lock)
            {
                _receiveQueue.Enqueue(data);
            }
        }

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
                return Task.FromResult(toRead);
            }
        }

        public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            var sent = new byte[count];
            Array.Copy(data, offset, sent, 0, count);

            // 触发回调，让测试代码有机会 EnqueueReceive
            OnSend?.Invoke(sent);

            return Task.CompletedTask;
        }

        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                _currentReceive.Clear();
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _isOpen = false;
            _receiveQueue.Clear();
            _currentReceive.Clear();
        }
    }
}

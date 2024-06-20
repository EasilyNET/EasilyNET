using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// RabbitMQ持久链接
/// </summary>
internal sealed class PersistentConnection : IPersistentConnection, IDisposable
{
    private readonly IConnectionFactory _connFactory;
    private readonly SemaphoreSlim _connLock = new(1, 1);
    private readonly ILogger<PersistentConnection> _logger;
    private readonly uint _poolCount;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1); // 用于控制重连并发的信号量
    private readonly RabbitConfig config;
    private ChannelPool? _channelPool;
    private IConnection? _connection;
    private bool _disposed;
    private bool _isReconnecting; // 用于控制重连尝试的标志位

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ILogger<PersistentConnection> logger)
    {
        _connFactory = connFactory ?? throw new ArgumentNullException(nameof(connFactory));
        config = options.Get(Constant.OptionName);
        _poolCount = config.PoolCount < 1 ? (uint)Environment.ProcessorCount : config.PoolCount;
        _logger = logger;
    }

    /// <inheritdoc />
    internal override bool IsConnected => _connection is not null && _connection.IsOpen;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_connection is not null)
            {
                try
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.CallbackException -= OnCallbackException;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("{Message}", ex.Message);
                }
            }
            if (_channelPool is null) return;
            try
            {
                _channelPool.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("{Message}", ex.Message);
            }
        }
        _disposed = true;
    }

    /// <inheritdoc />
    internal override async Task<IChannel> GetChannel() =>
        !IsConnected
            ? throw new InvalidOperationException("RabbitMQ连接失败")
            : _connection is null
                ? throw new InvalidOperationException("RabbitMQ连接未创建")
                : _channelPool is null
                    ? throw new("通道池为空")
                    : await _channelPool.GetChannel();

    /// <inheritdoc />
    internal override async Task ReturnChannel(IChannel channel)
    {
        if (_channelPool is null) throw new("通道池为空");
        await _channelPool.ReturnChannel(channel);
    }

    /// <inheritdoc />
    internal override async Task TryConnect()
    {
        _logger.LogInformation("RabbitMQ客户端尝试连接");
        await _connLock.WaitAsync();
        try
        {
            var policy = Policy.Handle<SocketException>()
                               .Or<BrokerUnreachableException>()
                               .WaitAndRetryAsync(config.RetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   _logger.LogWarning(ex, "RabbitMQ客户端在 {TimeOut}s 超时后无法创建链接,({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message));
            await policy.ExecuteAsync(async () => _connection = config.AmqpTcpEndpoints is not null && config.AmqpTcpEndpoints.Count > 0 ? await _connFactory.CreateConnectionAsync(config.AmqpTcpEndpoints) : await _connFactory.CreateConnectionAsync());
        }
        finally
        {
            // 先移除事件,以避免重复注册
            if (_connection is not null)
            {
                _connection.ConnectionShutdown -= OnConnectionShutdown;
                _connection.CallbackException -= OnCallbackException;
                _connection.ConnectionBlocked -= OnConnectionBlocked;
            }
            if (IsConnected && _connection is not null)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                _logger.LogInformation("RabbitMQ客户端与 {HostName} 建立了连接", _connection.Endpoint.HostName);
                _channelPool = new(_connection, _poolCount);
                _logger.LogInformation("RabbitBus channel pool count: {Count}", _poolCount);
            }
            else
            {
                _logger.LogCritical("RabbitMQ连接不能被创建和打开");
            }
            _connLock.Release();
        }
    }

    /// <summary>
    /// 重连
    /// </summary>
    /// <returns></returns>
    private async Task TryReconnect()
    {
        if (_isReconnecting) return;      // 如果已经在尝试重连,则直接返回
        await _reconnectLock.WaitAsync(); // 请求重连锁
        try
        {
            if (_isReconnecting) return; // 再次检查标志位,以避免重复重连
            _isReconnecting = true;      // 设置标志位,表示开始重连尝试
            _logger.LogWarning("RabbitMQ客户端尝试重新连接");
            await TryConnect(); // 尝试重新连接
        }
        finally
        {
            _isReconnecting = false;  // 重置标志位,表示重连尝试结束
            _reconnectLock.Release(); // 释放重连锁
        }
    }

    private async void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e) => await TryReconnect();

    private async void OnCallbackException(object? sender, CallbackExceptionEventArgs e) => await TryReconnect();

    private async void OnConnectionShutdown(object? sender, ShutdownEventArgs reason) => await TryReconnect();
}
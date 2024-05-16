﻿using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace N8.Shared.Messaging;

public class MessageQueueService : BackgroundService
{
    private readonly RabbitMqConsumer _consumer;
    private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();

    public event Action<string> OnMessageReceived;

    public MessageQueueService(IConnectionFactory connectionFactory)
    {
        _consumer = new RabbitMqConsumer(connectionFactory);
        _consumer.OnMessageReceived += HandleMessageReceived;
    }

    private void HandleMessageReceived(string message)
    {
        _messages.Enqueue(message);
        OnMessageReceived?.Invoke(message);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public ConcurrentQueue<string> GetMessages() => _messages;

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}

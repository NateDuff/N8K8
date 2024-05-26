using Azure.Messaging.ServiceBus;
using N8.Shared.Saga;
using System.Text.Json.Serialization;

namespace N8.Shared.Serializers;

[JsonSerializable(typeof(Todo[]))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{

}

[JsonSerializable(typeof(CustomerRequest))]
public partial class CustomerRequestSerializerContext : JsonSerializerContext
{

}

[JsonSerializable(typeof(SagaMessage))]
public partial class SagaMessageSerializerContext : JsonSerializerContext
{

}

[JsonSerializable(typeof(ServiceBusMessage))]
public partial class ServiceBusMessageSerializerContext : JsonSerializerContext
{

}
module Newsletter.API.Messaging.Publisher

open System
open System.Text.Json

open RabbitMQ.Client

open Newsletter.Core
open Newsletter.API.Messaging.Types

type Publisher (connection:IConnection, logger: Serilog.ILogger, config:RootConfig) =
    let mutable channel = connection.CreateModel()
    let mutable disposed = false

    member this.DispatchCancelSubscriptionEvent(event:CancelSubscriptionEventDTO) =
        let body = JsonSerializer.SerializeToUtf8Bytes event |> ReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = CANCEL_SUBSCRIBER_ROUTING_KEY,
            basicProperties = props,
            body = body
        )

    member this.DispatchUpdateSubscriptionEvent(event:UpdateSubscriptionEventDTO) =
        let body = JsonSerializer.SerializeToUtf8Bytes event |> ReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = UPDATE_SUBSCRIBER_ROUTING_KEY,
            basicProperties = props,
            body = body
        )

    member this.DispatchAddSubscriptionEvent(event:AddSubscriptionEventDTO) =
        let body = JsonSerializer.SerializeToUtf8Bytes event |> ReadOnlyMemory
        let props = channel.CreateBasicProperties()
        props.Persistent <- true
        
        channel.BasicPublish(
            exchange = config.Exchange,
            routingKey = ADD_SUBSCRIBER_ROUTING_KEY,
            basicProperties = props,
            body = body
        )
   
    member this.Dispose(disposing:bool) =
        if not disposed then    
            if disposing then
                channel.Dispose()
                channel <- null
            disposed <- true
    
    interface IDisposable with
        member this.Dispose() =
            this.Dispose(true)
            GC.SuppressFinalize(this)
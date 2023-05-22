module Newsletter.API.Messaging.Consumer

open System
open System.Text.Json
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options

open RabbitMQ.Client

open Newsletter.Core
open Newsletter.API.Messaging.Types

let handleEvent<'T>
    (logger: Serilog.ILogger)
    (channel: IModel)
    (routingKey: string)
    (redelivered: bool)
    (deliveryTag: uint64)
    (payload: Result<'T, string>)
    (action: 'T -> bool -> unit) =
        logger.Information(sprintf "Del tag: %i" deliveryTag)
        match payload with
        | Ok event ->
            logger.Debug(sprintf "Handling internal event with routing key %s: %O" routingKey event)
            action event redelivered
            logger.Information("Action completed")
            // if handling event doesn't throw, acknowledge message
            channel.BasicAck(deliveryTag, false)
            logger.Information("Ack")

        | Error err ->
            logger.Error(err)
            // if event is invalid allow to retry once in case of message corruption
            channel.BasicReject(deliveryTag, not redelivered)

type Consumer(channel:IModel, logger: Serilog.ILogger, provider:IServiceProvider) =

    inherit DefaultBasicConsumer(channel)

    let config = provider.GetRequiredService<IOptions<RootConfig>>().Value
    
    member this.HandleEvent<'T>
        (routingKey:string)
        (redelivered: bool)
        (deliveryTag: uint64)
        (payload:Result<'T, string>)
        (action: 'T -> bool -> unit)
        = handleEvent<'T> logger channel routingKey redelivered deliveryTag payload action

    override this.HandleBasicDeliver
        ( _consumerTag: string
        , deliveryTag: uint64
        , redelivered: bool
        , _exchange: string
        , routingKey: string
        , _properties: IBasicProperties
        , body: ReadOnlyMemory<byte>
        ) =
        try
            use scope = provider.CreateScope()
            let store = scope.ServiceProvider.GetRequiredService<ISubscriberStore>()

            match routingKey with
            | UPDATE_SUBSCRIBER_ROUTING_KEY ->
                let dto:Result<UpdateSubscriptionEventDTO, string> =
                    try
                        logger.Information("deserializing")
                        JsonSerializer.Deserialize<UpdateSubscriptionEventDTO>(body.ToArray()) |> Ok
                    with
                        | _ -> Error "Could not deserialize the event."
                    
                match (dto |> Result.toOption) with
                | None -> 
                    Error "DTO object was empty." |> ignore
                    ()
                | Some item ->
                    let update = UpdateSubscriptionEventDTO.toUpdate item
                    logger.Information($"DTO serialized successfully: %b{dto |> Result.isOk}" )
                    this.HandleEvent routingKey redelivered deliveryTag dto (fun x _ -> (store.UpdateSubscriptionAsync x.CurrentEmail update) |> Async.RunSynchronously |> ignore)
            
            | CANCEL_SUBSCRIBER_ROUTING_KEY ->
                let dto:Result<CancelSubscriptionEventDTO, string> =
                    try
                        logger.Information("deserializing")
                        JsonSerializer.Deserialize<CancelSubscriptionEventDTO>(body.ToArray()) |> Ok
                    with
                        | _ -> Error "Could not deserialize the event."

                logger.Information($"DTO serialized successfully: %b{dto |> Result.isOk}" )
                this.HandleEvent routingKey redelivered deliveryTag dto (fun x _ -> store.DeleteSubscriptionAsync x.Email |> Async.RunSynchronously |> ignore)

            | ADD_SUBSCRIBER_ROUTING_KEY ->
                let dto =
                    try
                        logger.Information("deserializing")
                        JsonSerializer.Deserialize<AddSubscriptionEventDTO>(body.ToArray()) |> Ok
                    with
                        | _ -> Error "Could not deserialize the event."
                    
                logger.Information($"DTO serialized successfully: %b{dto |> Result.isOk}" )
                
                let newSub:Result<CreateSubscriber, string> = 
                    dto |> Result.map (fun x -> {
                        Name = x.Name
                        Email = x.Email
                    })
                
                this.HandleEvent routingKey redelivered deliveryTag newSub (fun x _bool -> store.AddSubscriberAsync x |> Async.RunSynchronously |> ignore)
            
            | _ -> channel.BasicReject(deliveryTag, false)
        with
            | _ -> channel.BasicReject(deliveryTag, true)
            
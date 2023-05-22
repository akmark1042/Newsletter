module Newsletter.API.Http.Handler

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

open FSharp.Control.TaskBuilder

open Giraffe

open Newsletter.API.Messaging.Publisher
open Newsletter.API.Messaging.Types
open Newsletter.Core
open Types

let handleGetSubscribersAsync =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! store = ctx.GetService<ISubscriberStore>().GetSubscribersAsync()
        let allSubscribers = store |> List.map SubscriberDTO.fromDomain
        return! (json allSubscribers) next ctx
    }

let handleGetByIdAsync (idx: int) =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! subscriber = ctx.GetService<ISubscriberStore>().GetSubscriberByIndexAsync idx

        match subscriber with
        | None -> return! RequestErrors.NOT_FOUND "Subscriber did not exist" next ctx
        | Some sub -> 
            let results = SubscriberDTO.fromDomain sub
            return! (json results) next ctx
    }

let handleGetByEmailAsync (id: string) =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! subscriber = ctx.GetService<ISubscriberStore>().GetSubscriberByEmailAsync id
        match subscriber with
        | None -> return! RequestErrors.NOT_FOUND "Subscriber did not exist" next ctx
        | Some sub -> 
            let results = SubscriberDTO.fromDomain sub
            return! (json results) next ctx
    }

let handleAddSubscriberAsync : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        try
            let store = ctx.GetService<ISubscriberStore>()
            let! bindSub = ctx.BindJsonAsync<SubscriberDTO>()
            match bindSub.HasErrors() with //Validation
            | Some errors -> return! (RequestErrors.BAD_REQUEST errors next ctx)
            | _ -> 
                let! result = store.GetSubscriberByEmailAsync bindSub.Email
                match result with
                | Some _ -> return! RequestErrors.CONFLICT "Subscriber already exists" next ctx
                | None ->
                    let switch = SubscriberDTO.toCreate bindSub
                    let event:AddSubscriptionEventDTO = {
                            Email = switch.Email
                            Name = switch.Name
                        }

                    let publisher = ctx.GetService<Publisher>()
                    publisher.DispatchAddSubscriptionEvent(event)

                    return! Successful.NO_CONTENT next ctx
        with
            | ex -> return! (RequestErrors.BAD_REQUEST ex next ctx)
    }

// {
//     "NewName": "alex",
//     "NewEmail": "alex@alex.com",
//     "email": "joe@joe.com"
// }

// {
//     "id": 38,
//     "name": "joe",
//     "email": "joe@joe.com"
// }

let handleUpdateSubscriberAsync (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        try
            let store = ctx.GetService<ISubscriberStore>()
            let loggerA = ctx.GetLogger<ISubscriberStore>()
            // let! bindtest = ctx.BindJsonAsync<UpdateSubscriptionEventDTO>()
            let! bindSub = ctx.BindJsonAsync<UpdateSubscriber>()
            loggerA.LogInformation("Begin validation")
            match bindSub.HasErrors() with //Validation
            | Some errors -> return! (RequestErrors.BAD_REQUEST errors next ctx)
            | _ ->
                let event:UpdateSubscriptionEventDTO = {
                    Email = bindSub.NewEmail
                    Name = Some bindSub.NewName
                    CurrentEmail = id
                }

                let publisher = ctx.GetService<Publisher>()
                publisher.DispatchUpdateSubscriptionEvent(event)

                return! Successful.NO_CONTENT next ctx
        with
            | ex ->
                printfn "Handler error message: %s" ex.Message
                return! (RequestErrors.BAD_REQUEST ex next ctx)
    }

let handleDeleteSubscriberAsync (email:string) =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let event:CancelSubscriptionEventDTO = {
                Email = email
            }

        let publisher = ctx.GetService<Publisher>()
        publisher.DispatchCancelSubscriptionEvent(event)


        return! Successful.NO_CONTENT next ctx
    }
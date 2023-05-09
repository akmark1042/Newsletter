module Newsletter.API.Http.Handler

open Microsoft.AspNetCore.Http

open FSharp.Control.TaskBuilder
open Giraffe

open Newsletter.Core
open Types

let handleGetSubscribersAsync =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let! store = ctx.GetService<ISubscriberStore>().GetSubscribersAsync()
        let allSubscribers = store |> List.map SubscriberDTO.fromDomain
        return! (json allSubscribers) next ctx
    }

let handleAddSubscriberAsync : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        try
            let store = ctx.GetService<ISubscriberStore>()
            let! bindSub = ctx.BindJsonAsync<SubscriberDTO>()
            match bindSub.HasErrors() with //Validation
            | Some errors -> return! (RequestErrors.BAD_REQUEST errors next ctx)
            | _ -> 
                let switch = SubscriberDTO.toCreate bindSub
                let! result = store.AddSubscriberAsync switch
                match result with
                | 0 -> return! RequestErrors.CONFLICT "Subscriber already exists" next ctx
                | _ -> return! Successful.NO_CONTENT next ctx
        with
            | ex -> return! (RequestErrors.BAD_REQUEST ex next ctx)
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

let handleUpdateSubscriberAsync (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        try
            let! bindSub = ctx.BindJsonAsync<UpdateSubscriber>()
            match bindSub.HasErrors() with //Validation
            | Some errors -> return! (RequestErrors.BAD_REQUEST errors next ctx)
            | _ -> 
                let store = ctx.GetService<ISubscriberStore>()
                let! result = store.UpdateSubscriptionAsync id bindSub
                match result with
                | 0 -> return! RequestErrors.NOT_FOUND "Subscriber did not exist" next ctx
                | 1 -> return! RequestErrors.CONFLICT "Subscriber already exists" next ctx
                | _ -> return! Successful.NO_CONTENT next ctx
        with
            | ex -> return! (RequestErrors.BAD_REQUEST ex next ctx)
    }

let handleDeleteSubscriberAsync str =
    fun (next: HttpFunc) (ctx: HttpContext) -> task {
        let store = ctx.GetService<ISubscriberStore>()
        let! _numberDeleted = store.DeleteSubscriptionAsync str
        match _numberDeleted with
        | 1 -> return! Successful.NO_CONTENT next ctx
        | _ -> return! RequestErrors.NOT_FOUND "Subscriber did not exist" next ctx
    }
[<AutoOpen>]
module Newsletter.Core.Types

open System.Text.RegularExpressions

open System
open Giraffe

let private regX = Regex(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$")

type MenuOption =
    | AddSubscriber
    | DisplaySubscribers
    | SubscriberSearch
    | UpdateSubscription
    | EndSubscription
    | Exit

type Subscriber =
    {
        Id: int
        Name: string option
        Email: string
    }

type CreateSubscriber =
    {
        Name: string option
        Email: string
    }
    member this.HasErrors() =
        if regX.IsMatch( this.Email )
            then None
        else
            Some "An invalid email address was entered."
    
    interface IModelValidation<CreateSubscriber> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.BAD_REQUEST msg)
            | None     -> Ok this

type UpdateSubscriber =
    {
        NewName: string
        NewEmail: string
    }
    member this.HasErrors() =
        printfn "Beginning validation"
        if regX.IsMatch( this.NewEmail )
            then None
        else
            Some "An invalid email address was entered."
    
    interface IModelValidation<UpdateSubscriber> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.BAD_REQUEST msg)
            | None     -> Ok this

[<CLIMutable>]
type RabbitMQConfig =
    {
        Hosts: string seq
        ClusterFQDN: string
        VirtualHost: string
        SSL: bool
        Username: string
        Password: string
    }

[<CLIMutable>]
type DatabaseConfig =
    {
        ConnectionString : string
    }

[<CLIMutable>]
type NewsletterConfig =
    {
        Token: string
    }

[<CLIMutable>]
type RootConfig =
    {
        Database : DatabaseConfig
        Newsletter : NewsletterConfig
        RabbitMQConnection : RabbitMQConfig
        Exchange: string
        Queue: string
    }

type AuthToken = AuthToken of String

module AuthToken =
    let unwrap (AuthToken token) = token

    let validate (headers:string list) =
        match headers with
        | [] -> Error "No headers provided."
        | (value::_) -> Ok value
    
    let getToken (header:string) =
        if header.StartsWith("Basic ")
        then header.Replace("Basic ", "") |> Ok
        else Error "Not a basic authentication token."
    
    let make (authorization:string list) =
        authorization |> validate |> Result.bind getToken |> Result.map AuthToken

[<AutoOpen>]
module Newsletter.Core.Types

open System
open Giraffe

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

type CreateSubscriber = //used only in one place
    {
        Name: string option
        Email: string
    }
    member this.HasErrors() =
        if this.Email.Length = 0 then Some "Invalid entry." //Add regex for email address checking .com/.net etc.
        else None
    
    interface IModelValidation<CreateSubscriber> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.BAD_REQUEST msg)
            | None     -> Ok this

type UpdateSubscriber = //used only in one place, gets ID from the URL
    {
        NewName: string option
        NewEmail: string
    }
    member this.HasErrors() =
        if this.NewEmail.Length = 0 then Some "Invalid entry." //Add regex for email address checking .com/.net etc.
        else None
    
    interface IModelValidation<UpdateSubscriber> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.BAD_REQUEST msg)
            | None     -> Ok this

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

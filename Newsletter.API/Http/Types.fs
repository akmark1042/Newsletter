module Newsletter.API.Http.Types

open System.Text.RegularExpressions

open Giraffe

open Newsletter.Core

let private regX = Regex(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$")

type SubscriberDTO =
    {
        Id: int
        Name: string
        Email: string
    }
    member this.HasErrors() =
        if regX.IsMatch( this.Email )
            then None
        else
            Some "An invalid email address was entered."
    
    interface IModelValidation<SubscriberDTO> with
        member this.Validate() =
            match this.HasErrors() with
            | Some msg -> Error (RequestErrors.BAD_REQUEST msg)
            | None     -> Ok this

module SubscriberDTO =
    let fromDomain (subscriber: Subscriber) =
        {
            Id = subscriber.Id
            Name = (Option.defaultValue "" subscriber.Name)
            Email = subscriber.Email
        }
    
    let toCreate (subscriber: SubscriberDTO) : CreateSubscriber =
        {
            Name = Some subscriber.Name
            Email = subscriber.Email
        }
        
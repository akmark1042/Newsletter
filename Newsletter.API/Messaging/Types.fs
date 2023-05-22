module Newsletter.API.Messaging.Types

open Newsletter.Core

type CancelSubscriptionEventDTO =
    {
        Email: string
    }

type UpdateSubscriptionEventDTO =
    {
        Name: string option
        Email: string
        CurrentEmail: string
    }

module UpdateSubscriptionEventDTO =
    let toUpdate (subscriber: UpdateSubscriptionEventDTO) : UpdateSubscriber =
        {
            NewName = (Option.defaultValue "" subscriber.Name)
            NewEmail = subscriber.Email
        }

type AddSubscriptionEventDTO =
    {
        Name: string option
        Email: string
    }

module AddSubscriptionEventDTO =
    let toCreate (subscriber: AddSubscriptionEventDTO) : CreateSubscriber =
        {
            Name = subscriber.Name
            Email = subscriber.Email
        }

[<Literal>]
let ADD_SUBSCRIBER_ROUTING_KEY = "add.subscriber"

[<Literal>]
let CANCEL_SUBSCRIBER_ROUTING_KEY = "cancel.subscriber"

[<Literal>]
let UPDATE_SUBSCRIBER_ROUTING_KEY = "update.subscriber"

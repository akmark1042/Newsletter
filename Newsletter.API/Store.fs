module Newsletter.API.Store

open Newsletter.Core
open Newsletter.API.Database

type SubscriberStore (db:SubscriberDatabase) =
    interface ISubscriberStore with
        member this.GetSubscribersAsync() = getSubscribersAsync db

        member this.AddSubscriberAsync sub = addSubscriberAsync db sub

        member this.GetSubscriberByIndexAsync idx = getSubscriberByIdAsync db idx

        member this.GetSubscriberByEmailAsync email = getSubscriberByEmailAsync db email

        member this.UpdateSubscriptionAsync id sub = updateSubscriberAsync db id sub

        member this.DeleteSubscriptionAsync sub = deleteSubscriberAsync db sub

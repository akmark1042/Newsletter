[<AutoOpen>]
module Newsletter.Core.Interfaces

open Newsletter.Core.Types

type ISubscriberStore =
    abstract GetSubscribersAsync: unit -> Async<Subscriber list>
    abstract AddSubscriberAsync: CreateSubscriber -> Async<int>
    abstract GetSubscriberByIndexAsync: int -> Async<Subscriber option>
    abstract GetSubscriberByEmailAsync: string -> Async<Subscriber option>
    abstract DeleteSubscriptionAsync: string -> Async<int>
    abstract UpdateSubscriptionAsync: string -> UpdateSubscriber -> Async<int>
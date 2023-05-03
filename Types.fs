[<AutoOpen>]
module Newsletter.Types

type MenuOption =
    | AddSubscriber
    | DisplaySubscribers
    | UpdateSubscription
    | EndSubscription 
    | Exit

type Subscriber =
    {
        Name: string option
        Email: string
    }
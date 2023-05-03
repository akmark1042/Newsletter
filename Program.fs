module Newsletter.Program

open Newsletter.Database
open Newsletter.Menu
open Newsletter.Types

let ctx = Database.GetDataContext()
ctx.``Design Time Commands``.SaveContextSchema |> ignore

let rec loop (db:SubscriberDatabase) =
    match getMenuOption() with
    | AddSubscriber ->
        addSubscriber db (getSubscriberInfo ())
        loop db
    | DisplaySubscribers ->
        getSubscribers db |> showAllSubscribers
        loop db
    | UpdateSubscription ->
        let subscriber = getSubscriberEmail() |> getSubscriberByEmail db
        match subscriber with
        | None ->
            printfn "No subscriptions were found."
        | Some sub -> 
            printfn "Your account has been found.\n"
            showSubscription sub
            let newCredentials = getSubscriberInfo ()
            updateSubscriber db newCredentials
        loop db
    | EndSubscription ->
        let email = getSubscriberEmail ()
        match (deleteSubscriber db email) with
        | 1 ->
            printfn "This subscription has been canceled. \n"
        | _zero -> printfn "No subscriptions were found. \n"
        loop db
    | Exit -> ()

[<EntryPoint>]
let main args =
    let db = new SubscriberDatabase()
    loop db
    0
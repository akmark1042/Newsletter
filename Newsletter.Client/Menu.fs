[<AutoOpen>]
module Newsletter.Client.Menu

open System

open Newsletter.Core.Types

let rec getSearch() =
    printfn "Enter an id or email address:"
    let userInput = Console.ReadLine()
    match userInput with
    | input when input.Length > 0 ->
        userInput //return
    | _ ->
        printfn "Invalid entry."
        getSearch()

let getSubscriberName() =
    printfn "Enter your name:"
    Console.ReadLine()

let getSubscriberEmail() =
    printfn "Enter your email:"
    Console.ReadLine()

let getSubscriberInfo() =
    let name = getSubscriberName()
    let email = getSubscriberEmail()

    {
        Name = Some name
        Email = email
    }

let getUpdateInfo =
    let updName = getSubscriberName()
    let updEmail = getSubscriberEmail()

    {
        NewName = updName
        NewEmail = updEmail
    }

let showSubscription (sub: Subscriber) =
    printfn "%i. %s - %s" sub.Id (Option.defaultValue "" sub.Name) sub.Email

let showAllSubscribers (sublist : Subscriber list) =
    printfn ""
    if sublist.Length = 0 then
        printfn "No items found"
    else
        sublist |> List.iter showSubscription

let instructions() =
    printfn "\nChoose an option:"
    printfn "1. Add a subscriber"
    printfn "2. List all current subscribers"
    printfn "3. Subscriber search"
    printfn "4. Update your subscription"
    printfn "5. Cancel a subscription"
    printfn "6. End the program \n"

let rec getMenuOption() =
    instructions()
    let input = Console.ReadLine()
    match input with
    | "1" -> AddSubscriber
    | "2" -> DisplaySubscribers
    | "3" -> SubscriberSearch
    | "4" -> UpdateSubscription
    | "5" -> EndSubscription
    | "6" -> Exit
    | _ when input.ToLower().Contains("add") -> AddSubscriber
    | _ when input.ToLower().Contains("list") -> DisplaySubscribers
    | _ when input.ToLower().Contains("list") -> SubscriberSearch
    | _ when input.ToLower().Contains("update") -> UpdateSubscription
    | _ when input.ToLower().Contains("end") -> EndSubscription
    | _ when input.ToLower().Contains("exit") -> Exit
    | _otherwise ->
        printfn "Invalid input. Please try again."
        getMenuOption()
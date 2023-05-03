[<AutoOpen>]
module Newsletter.Menu

open System

open Newsletter.Database
open Newsletter.Types

let getSubscriberName () =
    printfn "Enter your name:"
    Console.ReadLine()

let getSubscriberEmail () =
    printfn "Enter your email:"
    Console.ReadLine()

let getSubscriberInfo () =
    let name = getSubscriberName ()
    let email = getSubscriberEmail ()

    {
        Name = Some name
        Email = email
    }

let showSubscription (sub: Subscriber) =
    printfn "%s - %s"  (Option.defaultValue "" sub.Name) sub.Email

let showAllSubscribers (sublist : Subscriber list) =
    printfn ""
    sublist |> List.iter showSubscription

let instructions () =
    printfn "\nChoose an option:"
    printfn "1. Add a subscriber"
    printfn "2. List all current subscribers"
    printfn "3. Update your subscription"
    printfn "4. Cancel a subscription"
    printfn "5. End the program \n"

let rec getMenuOption () =
    instructions()
    let input = Console.ReadLine()
    match input with
    | "1" -> AddSubscriber
    | "2" -> DisplaySubscribers
    | "3" -> UpdateSubscription
    | "4" -> EndSubscription
    | "5" -> Exit
    | _ when input.ToLower().Contains("add") -> AddSubscriber
    | _ when input.ToLower().Contains("list") -> DisplaySubscribers
    | _ when input.ToLower().Contains("update") -> UpdateSubscription
    | _ when input.ToLower().Contains("end") -> EndSubscription
    | _ when input.ToLower().Contains("exit") -> Exit
    | _otherwise ->
        printfn "Invalid input. Please try again."
        getMenuOption()
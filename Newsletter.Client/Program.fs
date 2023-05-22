module Newsletter.Client.Program

open System
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration

open Newsletter.Client.Menu
open Newsletter.Core.Types
open Newsletter.Client.Store

// --------------------------------- 
// Config Helpers 
// ---------------------------------

let configureAppConfiguration fn (builder: IHostBuilder) = 
    builder.ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> (fun ctx bldr -> fn ctx bldr |> ignore)) 

let configureServices fn (builder: IHostBuilder) = 
    builder.ConfigureServices(Action<HostBuilderContext, IServiceCollection> (fun ctx svc -> fn ctx svc |> ignore)) 

let build (bldr: IHostBuilder) = bldr.Build() 

let run (host: IHost) = host.Run()

// --------------------------------- 
// Config and Main
// ---------------------------------

let withServices bldr =
    bldr |> configureServices (fun context services ->
        services
            .Configure<RootConfig>(context.Configuration)
        )

let rec loopAsync() =
    async {
        match getMenuOption() with
        | AddSubscriber ->
            let userInfo = getSubscriberInfo()
            let! result = addSubscriberAsync userInfo
            match result with
            | 1 ->
                printfn "This user already exists. \n"
            | _zero -> let msg = "" + (Option.defaultValue "" userInfo.Name) + " was added."
                       printfn "%s" msg
            return! loopAsync()
        | DisplaySubscribers ->
            let! allList = getSubscribersAsync()
            do showAllSubscribers (List.sort allList)
            return! loopAsync()
        | SubscriberSearch ->
            let input = getSearch().ToString()
            let! result =
                match (System.Int32.TryParse input) with
                | true, v -> getByIdAsync v
                | false, _ -> getByEmailAsync input
            match result with
            | None -> printfn "No matching item was found."
            | Some item ->
                showSubscription item
            return! loopAsync()
        | UpdateSubscription ->
            let email = getSubscriberEmail()
            let! subscriber = getByEmailAsync email
            match subscriber with
            | None -> printfn "No matching item was found."
            | Some sub ->
                let newCredentials = getUpdateInfo
                let! result = updateSubscriberAsync sub.Email newCredentials
                match result with
                | 1 -> 
                    let msg = "" + newCredentials.NewName.ToString() + " was updated."
                    printfn "%s" msg
                | _zero -> printfn "No subscriptions were found. \n"
            return! loopAsync()
        | EndSubscription ->
            let email = getSubscriberEmail()
            let! result = deleteSubscriberAsync email
            match result with
            | 1 ->
                printfn "This subscription has been canceled. \n"
            | _zero -> printfn "No subscriptions were found. \n"
            return! loopAsync()
        | Exit -> exit(0)
    }

[<EntryPoint>]
let main args =
    async {
        // let host =
        //     Host.CreateDefaultBuilder(args)
        //     |> withServices
        //     |> build

        do! loopAsync()

        return 0
    } |> Async.RunSynchronously

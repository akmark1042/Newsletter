module Newsletter.API.Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

open Giraffe

open Newsletter.Core
open Newsletter.API.Routes
open Newsletter.API.Store

// ---------------------------------
// Error handler
// ---------------------------------
let errorHandler (ex: Exception) (logger: Microsoft.Extensions.Logging.ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")

    clearResponse
    >=> setStatusCode 500
    >=> text "An unhandled error occured."

// --------------------------------- 
// Config Helpers 
// ---------------------------------

type LocalWebHostBuilder =
    { Builder: IWebHostBuilder
      ConfigureFn: IApplicationBuilder -> IApplicationBuilder }

let withLocalBuilder builder = { Builder = builder; ConfigureFn = id }

let configureAppConfiguration fn (builder: LocalWebHostBuilder) =
    let bldr =
        builder.Builder.ConfigureAppConfiguration(
            Action<WebHostBuilderContext, IConfigurationBuilder>(fun ctx bldr -> fn ctx bldr |> ignore)
        )

    { builder with Builder = bldr }

let configureServices fn (builder: LocalWebHostBuilder) =
    let bldr =
        builder.Builder.ConfigureServices(
            Action<WebHostBuilderContext, IServiceCollection>(fun ctx svc -> fn ctx svc |> ignore)
        )

    { builder with Builder = bldr }

let configure (fn: IApplicationBuilder -> IApplicationBuilder) (builder: LocalWebHostBuilder) =
    let cfgFn = builder.ConfigureFn >> fn

    let bldr =
        builder.Builder.Configure(Action<IApplicationBuilder>(fun app -> cfgFn app |> ignore))

    { Builder = bldr; ConfigureFn = cfgFn }


let build (bldr: IHostBuilder) = bldr.Build() 

let run (host: IHost) = host.Run()

// --------------------------------- 
// Config and Main
// ---------------------------------

let withConfiguration (bldr: LocalWebHostBuilder) =
    bldr
    |> configureAppConfiguration (fun context config ->
        config
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true, true)
            .AddEnvironmentVariables()
        |> ignore)

let withGiraffe bldr =
    bldr
    |> configureServices (fun _ services -> services.AddGiraffe())
    |> configure (fun app ->
        let config = app.ApplicationServices.GetService<IOptions<RootConfig>>().Value.Newsletter
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()

        if not (env.IsDevelopment()) then
            app.UseGiraffeErrorHandler(errorHandler) |> ignore

        app.UseGiraffe(webApp config.Token)
        app)

let withServices bldr =
    bldr |> configureServices (fun context services ->
        services
            .Configure<RootConfig>(context.Configuration)
            .AddScoped<SubscriberDatabase>(fun provider ->
                let config = provider.GetRequiredService<IOptions<RootConfig>>()
                SubscriberDatabase(config.Value.Database)
            )
            .AddScoped<ISubscriberStore, SubscriberStore>()
    )

///////////////////////////////////////////////////////////////
// For saving the schema
// let ctx = Database.GetDataContext()
// ctx.``Design Time Commands``.SaveContextSchema |> ignore
//
///////////////////////////////////////////////////////////////


[<EntryPoint>]
let main args =
    async {
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
                |> withLocalBuilder
                |> withConfiguration
                |> withGiraffe
                |> withServices
                |> ignore
                )
            .Build()
            .Run()

        return 0
    } |> Async.RunSynchronously

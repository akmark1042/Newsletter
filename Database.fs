[<AutoOpen>]
module Newsletter.Database

open FSharp.Data.Sql

open Newsletter.Types

[<Literal>]
let connString = "Host=localhost;Database=subscriber;Username=subscriber;Password=password"

[<Literal>]
let contextSchemaPath =__SOURCE_DIRECTORY__ + "/database.schema"

type Database =
    SqlDataProvider<Common.DatabaseProviderTypes.POSTGRESQL,
        connString,
        UseOptionTypes=Common.NullableColumnType.OPTION,
        ContextSchemaPath=contextSchemaPath>

let toSubscriber (subscriberRow: Database.dataContext.``public.subscriberEntity``) : Subscriber =
    {
        Name = subscriberRow.Name
        Email = subscriberRow.Email
    }

type SubscriberDatabase () =
    let dataContext : Database.dataContext =
        Database.GetDataContext(connString, selectOperations = SelectOperations.DatabaseSide)

    member this.getDataContext() = dataContext

let getSubscribers (sdb: SubscriberDatabase) =
    let db = sdb.getDataContext()
    db.Public.Subscriber
    |> Seq.map toSubscriber
    |> Seq.toList

let getSubscriberByEmail(sdb: SubscriberDatabase) (email:string) =
    let db = sdb.getDataContext()
    query {
        for row in db.Public.Subscriber do
            where (row.Email = email)
            select row
    }
    |> Seq.tryExactlyOne
    |> Option.map toSubscriber

let updateSubscriber (sdb: SubscriberDatabase) (sub:Subscriber) =
    let db = sdb.getDataContext()
    let rowMaybe = query {
        for row in db.Public.Subscriber do
            where (row.Email = sub.Email)
            select (Some row)
            exactlyOneOrDefault
    }
    match rowMaybe with
    | None -> printfn "No subscription is associated with that email address."
    | Some subscriber ->
        try
            subscriber.Name <- sub.Name
            subscriber.Email <- sub.Email
            subscriber.OnConflict <- FSharp.Data.Sql.Common.OnConflict.Update
            db.SubmitUpdates()
        with
            | _e -> printfn "This email address already exists.  Please enter another email address"

let addSubscriber (sdb: SubscriberDatabase) (sub:Subscriber) =
    try
        let db = sdb.getDataContext()
        let row = db.Public.Subscriber.Create()
        row.Name <- sub.Name
        row.Email <- sub.Email
        row.OnConflict <- FSharp.Data.Sql.Common.OnConflict.Update
        db.SubmitUpdates()
    with
        | _e -> printfn "This account already exists.  Please log in to update."

let deleteSubscriber (sdb: SubscriberDatabase) (email:string) : int =
    let db = sdb.getDataContext()
    let rowMaybe = query {
        for row in db.Public.Subscriber do
            where (row.Email = email)
            select (Some row)
            exactlyOneOrDefault
    }
    match rowMaybe with
    | None -> 0
    | Some subscriber ->
        subscriber.Delete()
        db.SubmitUpdates()
        1

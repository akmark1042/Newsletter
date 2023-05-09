[<AutoOpen>]
module Newsletter.API.Database

open FSharp.Data.Sql

open Newsletter.Core.Types

[<Literal>]
let connString = "Host=localhost;Database=subscriber;User ID=subscriber;Password=password"

[<Literal>]
let contextSchemaPath =__SOURCE_DIRECTORY__ + "/database.schema"

type Database =
    SqlDataProvider<Common.DatabaseProviderTypes.POSTGRESQL,
        connString,
        UseOptionTypes=Common.NullableColumnType.OPTION,
        ContextSchemaPath=contextSchemaPath>

let toSubscriber (subscriberRow: Database.dataContext.``public.subscriberEntity``) : Subscriber =
    {
        Id = subscriberRow.Id
        Name = subscriberRow.Name
        Email = subscriberRow.Email
    }

type SubscriberDatabase (config:DatabaseConfig) =
    let dataContext : Database.dataContext =
        Database.GetDataContext(config.ConnectionString)

    member this.getDataContext() = dataContext

let getSubscribersAsync (sdb: SubscriberDatabase) =
    async {
        let db = sdb.getDataContext()
        let allRows = db.Public.Subscriber
        let allList = allRows |> Seq.map toSubscriber |> Seq.toList
        return allList
    }

let getSubscriberByEmailAsync (sdb: SubscriberDatabase) (email:string) =
    async {
        let db = sdb.getDataContext()
        let row =
            query {
                for row in db.Public.Subscriber do
                    where (row.Email = email)
                    select row
            }
            |> Seq.map toSubscriber
            |> Seq.tryExactlyOne
        return row
    }

let getSubscriberByIdAsync (sdb: SubscriberDatabase) (idx: int) =
    async {
        let db = sdb.getDataContext()
        let row =
            query {
                for row in db.Public.Subscriber do
                    where (row.Id = idx)
                    select row
            }
            |> Seq.map toSubscriber
            |> Seq.tryExactlyOne
        return row
    }

let updateSubscriberAsync (sdb: SubscriberDatabase) (id:string) (sub:UpdateSubscriber) =
    async {
        let db = sdb.getDataContext()
        let rowMaybe = query {
            for row in db.Public.Subscriber do
                where (row.Email = sub.NewEmail)
                select (Some row)
                exactlyOneOrDefault
        }
        match rowMaybe with
        | None -> return 0
        | Some subscriber ->
            try
                subscriber.Name <- sub.NewName
                subscriber.Email <- sub.NewEmail
                subscriber.OnConflict <- Common.OnConflict.Update
                db.SubmitUpdates()
                return 2
            with
                | _e -> return 1
    }

let addSubscriberAsync (sdb: SubscriberDatabase) (sub:CreateSubscriber) = 
    async {
        //search/get, if found, return 2. 
        //That would prevent update conflicts.
        let db = sdb.getDataContext()
        let row = db.Public.Subscriber.Create()
        try
            row.Name <- sub.Name
            row.Email <- sub.Email
            row.OnConflict <- Common.OnConflict.Update
            db.SubmitUpdates()
            return 1
        with
            | _e -> return 0
    }

let deleteSubscriberAsync (sdb: SubscriberDatabase) (email:string) =
    async {
        let db = sdb.getDataContext()
        let rowMaybe = query {
            for row in db.Public.Subscriber do
                where (row.Email = email)
                select (Some row)
                exactlyOneOrDefault
        }
        match rowMaybe with
        | None -> return 0
        | Some subscriber ->
            subscriber.Delete()
            db.SubmitUpdates()
            return 1
    }

module Newsletter.Client.Store

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Text
open System.Text.Json

open Newsletter.Core.Types

let getClient() =
    let result = new HttpClient()
    result.DefaultRequestHeaders.Authorization <- new AuthenticationHeaderValue("Basic", "stub")
    result.BaseAddress <- new Uri("http://localhost:5000/api/subscribers")
    result

let getByIdAsync idx =
    async {
        use client = getClient()
        let! response = sprintf "subscribers/get/%i" idx |> client.GetAsync |> Async.AwaitTask

        if response.StatusCode = HttpStatusCode.NotFound then
            return None
        elif response.IsSuccessStatusCode |> not then
            return raise (sprintf "API returned unexpected status code: %O." response.StatusCode |> NotImplementedException)
        else
            let! result = response.Content.ReadFromJsonAsync<Subscriber>() |> Async.AwaitTask
            return Some result
    }

let getByEmailAsync id =
    async {
        use client = getClient()
        let! response = sprintf "subscribers/get/%s" id |> client.GetAsync |> Async.AwaitTask
        
        if response.StatusCode = HttpStatusCode.NotFound then
            return None
        elif response.IsSuccessStatusCode |> not then
            return raise (sprintf "API returned unexpected status code: %O." response.StatusCode |> NotImplementedException)
        else
            let! result = response.Content.ReadFromJsonAsync<Subscriber>() |> Async.AwaitTask
            return Some result
    }

let addSubscriberAsync (sub:CreateSubscriber) =
    async {
        use client = getClient()
        let! response = client.PostAsJsonAsync("subscribers/create", sub) |> Async.AwaitTask
        
        if response.StatusCode = HttpStatusCode.NoContent then
            return 0
        else 
            return 1
    }

let getSubscribersAsync() =
    async {
        let client = getClient()
        let! allSubscribers = client.GetAsync("") |> Async.AwaitTask

        if allSubscribers.IsSuccessStatusCode |> not then
            return raise (sprintf "API returned unexpected status code: %O." allSubscribers.StatusCode |> NotImplementedException)
        else
            let! result = allSubscribers.Content.ReadFromJsonAsync<List<Subscriber>>() |> Async.AwaitTask
            return result
    }

let updateSubscriberAsync (email:string) (sub:UpdateSubscriber) =
    async {
        use client = getClient()
        let httpContent = new StringContent(JsonSerializer.Serialize(sub), Encoding.UTF8, "application/json")
        let! response = client.PutAsync(("subscribers/update/" + email), httpContent) |> Async.AwaitTask
        if response.StatusCode = HttpStatusCode.NotFound then
            return 0
        else
            return 1
    }

let deleteSubscriberAsync str =
    async {
        use client = getClient()
        let! response = sprintf "subscribers/cancel/%s" str |> client.DeleteAsync |> Async.AwaitTask
        if response.StatusCode = HttpStatusCode.NotFound then
            return 0
        else
            return 1
    }
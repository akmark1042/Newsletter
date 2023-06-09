module Newsletter.API.Http.Auth

open FSharp.Control.TaskBuilder
open Microsoft.AspNetCore.Http

open Giraffe

open Newsletter.Core.Types

let staticBasic token =
    fun (next:HttpFunc) (ctx:HttpContext) -> 
        task {
            match ctx.Request.Headers.Authorization |> Seq.toList |> AuthToken.make with
            | Ok authToken when (AuthToken.unwrap authToken) = token -> return! next ctx
            | _ -> return! (setStatusCode 401 >=> setHttpHeader "WWW-Authenticate" "Basic") earlyReturn ctx
        }

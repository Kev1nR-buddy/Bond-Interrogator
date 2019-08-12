open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared


let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let webApp = router {
    get "/api/init" (fun next ctx ->
        task {
            let counter = {Value = 42}
            return! json counter next ctx
        })
    get "/api/films" (fun next ctx ->
        task {
            let movieList = [ {SequenceId = 1; Title = "Dr. No"; Synopsis = "Dr.No synopsis"; Bond = "Sean Connery"; M = Some "Bernard Lee"; Q = None; TheEnemy = [{ Name = "Dr. No"; Actor = "Joseph Wiseman"}]; TheGirls = [{Name = "Honey Ryder"; Actor = "Ursula Andress"}]}
                              {SequenceId = 2; Title = "From Russia with Love"; Synopsis = "From Russia with Love synopsis"; Bond = "Sean Connery"; M = Some "Bernard Lee"; Q = None; TheEnemy = [{ Name = "Grant"; Actor = "Robert Shaw"};{ Name = "Rosa Klebb"; Actor = "Lotte Lenya"}]; TheGirls = [{Name = "Tatiana Romanova"; Actor = "Daniela Bianchi"}]}
                              {SequenceId = 3; Title = "Goldfinger"; Synopsis = "Goldfinger synopsis"; Bond = "Sean Connery"; M = Some "Bernard Lee"; Q = Some "Desmond Llewelyn"; TheEnemy = [{ Name = "Auric Goldfinger"; Actor = "Gert Frobe"}]; TheGirls = [{Name = "Pussy Galore"; Actor = "Honor Blackman"}]} ]
            return! json movieList next ctx
        })
}

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip
}

run app

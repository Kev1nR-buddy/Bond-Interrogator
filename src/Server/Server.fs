open System.IO
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared
open Microsoft.Azure.Cosmos.Table

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

<<<<<<< HEAD
let storageConnString = "CONNECT_STR" |> tryGetEnv |> Option.defaultValue ""
printfn "CONN_STR = %s" storageConnString
=======
let storageConnString = ""
>>>>>>> d225931b96317347b5d0671b26688056ac377796
let storageAccount = CloudStorageAccount.Parse(storageConnString)
// Create the table client.
let tableClient = storageAccount.CreateCloudTableClient()

let table = tableClient.GetTableReference("BondFilm")

let query =
    TableQuery().Where(
        TableQuery.GenerateFilterCondition(
            "PartitionKey", QueryComparisons.Equal, "BondFilm"))

let getEnemies bondFilmSequenceId =
    tableClient.GetTableReference("TheEnemy")
               .ExecuteQuery(
                   TableQuery().Where(sprintf "PartitionKey eq '%d'" bondFilmSequenceId))
    |> Seq.map (fun e -> { Name = e.Properties.["Character"].StringValue; Actor = e.Properties.["Actor"].StringValue })

let getGirls bondFilmSequenceId =
    tableClient.GetTableReference("TheGirls")
               .ExecuteQuery(
                   TableQuery().Where(sprintf "PartitionKey eq '%d'" bondFilmSequenceId))
    |> Seq.map (fun e -> { Name = e.Properties.["Character"].StringValue; Actor = e.Properties.["Actor"].StringValue })

let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let webApp = router {
    get "/api/films" (fun next ctx ->
        task {
            let movieList = table.ExecuteQuery(query)
                            |> Seq.map (fun f ->
                                            let sequenceId = int f.RowKey
                                            let title = if f.Properties.ContainsKey("Title") then f.Properties.["Title"].StringValue else ""
                                            let synopsis = if f.Properties.ContainsKey("Synopsis") then f.Properties.["Synopsis"].StringValue else ""
                                            let bond = if f.Properties.ContainsKey("Bond") then f.Properties.["Bond"].StringValue else ""
                                            let m = if f.Properties.ContainsKey("M") then Some (f.Properties.["M"].StringValue) else None
                                            let q = if f.Properties.ContainsKey("Q") then Some (f.Properties.["Q"].StringValue) else None
                                            let theEnemy = getEnemies sequenceId |> Seq.toList
                                            let theGirls = getGirls sequenceId |> Seq.toList

                                            {SequenceId = sequenceId; Title = title; Synopsis = synopsis;
                                             Bond = bond; M = m; Q = q; TheEnemy = theEnemy; TheGirls = theGirls})
                            |> Seq.toList (* <- NOTE this is important the encoder doesn't like IEnumerable need to convert to List *)

            return! json movieList next ctx
        })
    getf "/api/list-media/%s" (fun filmId next ctx ->
        task {
            return! json (AzureServices.listBondMedia filmId) next ctx
        })
    getf "/api/media-item-character/%s/%s" (fun (filmId, character) next ctx ->
        task {
            return! json (AzureServices.getBondMediaCharacterURI filmId character) next ctx
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

open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared
open Microsoft.Azure.Documents.Client
open Microsoft.Azure.Cosmos.Table


let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=kbrstorageaccount;AccountKey=9f3T1Fco4+c2N9zXI5plUL0sh67lEsxbeNlhHwQ1nRgYZutJeD1w7IQYSQhDtYx1Glb+QA18E/rUxqjtB2xz1g==;EndpointSuffix=core.windows.net"
let storageAccount = CloudStorageAccount.Parse(storageConnString)
// Create the table client.
let tableClient = storageAccount.CreateCloudTableClient()

let table = tableClient.GetTableReference("BondFilm")

let query =
    TableQuery().Where(
        TableQuery.GenerateFilterCondition(
            "PartitionKey", QueryComparisons.Equal, "BondFilm"))

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
            let movieList = table.ExecuteQuery(query)
                            |> Seq.map (fun f ->
                                            let title = if f.Properties.ContainsKey("Title") then f.Properties.["Title"].StringValue else ""
                                            let synopsis = if f.Properties.ContainsKey("Synopsis") then f.Properties.["Synopsis"].StringValue else ""
                                            let bond = if f.Properties.ContainsKey("Bond") then f.Properties.["Bond"].StringValue else ""
                                            let m = if f.Properties.ContainsKey("M") then Some (f.Properties.["M"].StringValue) else None
                                            let q = if f.Properties.ContainsKey("Q") then Some (f.Properties.["Q"].StringValue) else None

                                            {SequenceId = int f.RowKey; Title = title; Synopsis = synopsis;
                                             Bond = bond; M = m; Q = q; TheEnemy = []; TheGirls = []})
                            |> Seq.toList (* <- NOTE this is important the encoder doesn't like IEnumerable need to convert to List *)

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

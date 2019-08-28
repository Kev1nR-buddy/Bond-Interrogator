module AzureServices

open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared
open Microsoft.Azure.Documents.Client
open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let listBondMedia() =
    let storageConnString = "DefaultEndpointsProtocol=https;AccountName=kbrstorageaccount;AccountKey=9f3T1Fco4+c2N9zXI5plUL0sh67lEsxbeNlhHwQ1nRgYZutJeD1w7IQYSQhDtYx1Glb+QA18E/rUxqjtB2xz1g==;EndpointSuffix=core.windows.net"
    let storageAccount = CloudStorageAccount.Parse(storageConnString)
    // Create the table client.
    let blobClient = storageAccount.CreateCloudBlobClient()

    let cloudBlobContainer = blobClient.GetContainerReference("bond-film-media")

    let permissions = BlobContainerPermissions(PublicAccess=BlobContainerPublicAccessType.Blob)
    cloudBlobContainer.SetPermissions(permissions)

    // Loop over items within the container and output the length and URI.
    // NOTE arge the first Prefix arg is set to the folder we want to pull medai from
    // the second 'useFlatBlobListing' returns only blobs (not folders) when set to true
    let blobs = cloudBlobContainer.ListBlobs("DrNo", true)
                |> Seq.map (fun item ->
                        match item with
                        | :? CloudBlockBlob as blob ->
                            sprintf "Block blob of length %d: %O" blob.Properties.Length blob.Uri

                        | :? CloudPageBlob as pageBlob ->
                            sprintf "Page blob of length %d: %O" pageBlob.Properties.Length pageBlob.Uri

                        | :? CloudBlobDirectory as directory ->
                            sprintf "Directory: %O" directory.Uri

                        | _ ->
                            sprintf "Unknown blob type: %O" (item.GetType()))
    blobs |> Seq.toList
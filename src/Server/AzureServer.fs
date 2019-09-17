module AzureServices

open System.IO
open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let listBondMedia filmId =
    let storageConnString = "CONNECT_STR" |> tryGetEnv |> Option.defaultValue ""

    let storageAccount = CloudStorageAccount.Parse(storageConnString)

    // Create the table client.
    let blobClient = storageAccount.CreateCloudBlobClient()

    let cloudBlobContainer = blobClient.GetContainerReference("bond-film-media")

    let permissions = BlobContainerPermissions(PublicAccess=BlobContainerPublicAccessType.Blob)
    cloudBlobContainer.SetPermissions(permissions)

    // Loop over items within the container and output the length and URI.
    // NOTE arge the first Prefix arg is set to the folder we want to pull medai from
    // the second 'useFlatBlobListing' returns only blobs (not folders) when set to true
    let blobs = cloudBlobContainer.ListBlobs(filmId, true)
                |> Seq.map (fun item ->
                        printfn "Blob item of type %A" item
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

let listBondMediaBlobs filmId =
    let storageConnString = "CONNECT_STR" |> tryGetEnv |> Option.defaultValue ""

    let storageAccount = CloudStorageAccount.Parse(storageConnString)

    // Create the table client.
    let blobClient = storageAccount.CreateCloudBlobClient()

    let cloudBlobContainer = blobClient.GetContainerReference("bond-film-media")

    let permissions = BlobContainerPermissions(PublicAccess=BlobContainerPublicAccessType.Blob)
    cloudBlobContainer.SetPermissions(permissions)

    let blobs = cloudBlobContainer.ListBlobs(filmId, true)
                |> Seq.filter (fun item ->
                                match item with
                                | :? CloudBlockBlob -> true
                                | _ -> false)
                |> Seq.map (fun item -> item :?> CloudBlockBlob)

    blobs |> Seq.toList

let private isCharacter character (item : CloudBlockBlob) =
    item.FetchAttributes()
    item.Metadata.ContainsKey("Character") && item.Metadata.["Character"] = character

let getBondMediaCharacterURI sequenceId character =
    let blob =
        listBondMediaBlobs sequenceId
        |> Seq.filter (isCharacter character)
        |> Seq.tryHead
        |> Option.map (fun b -> b.Uri.AbsoluteUri)

    blob
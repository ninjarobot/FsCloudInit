namespace FsCloudInit

open System
open System.IO.Compression

module Builders =

    /// Builder for a WriteFile record.
    type WriteFileBuilder () =
        member _.Yield _ = WriteFile.Default
        [<CustomOperation "path">]
        member _.Path (writeFile:WriteFile, path:string) =
            { writeFile with Path = path }
        [<CustomOperation "content">]
        member _.Content (writeFile:WriteFile, content:string) =
            { writeFile with 
                Encoding = FileEncoding.Base64
                Content = content |> System.Text.Encoding.UTF8.GetBytes |> Convert.ToBase64String }
        [<CustomOperation "gzip_data">]
        member _.GZipData (writeFile:WriteFile, contentStream:System.IO.Stream) =
            use compressed = new System.IO.MemoryStream()
            using (new GZipStream(compressed, CompressionMode.Compress)) contentStream.CopyTo
            let content = compressed.ToArray() |> Convert.ToBase64String
            { writeFile with 
                Encoding = FileEncoding.GzipBase64
                Content = content }
        member this.GZipData (writeFile:WriteFile, content:byte []) =
            use ms = new System.IO.MemoryStream(content)
            this.GZipData (writeFile, ms)
        member this.GZipData (writeFile:WriteFile, content:string) =
            use ms = new System.IO.MemoryStream(content |> System.Text.Encoding.UTF8.GetBytes)
            this.GZipData (writeFile, ms)
        [<CustomOperation "base64_encoded_content">]
        member _.Base64EncodedContent (writeFile:WriteFile, content:string) = 
            { writeFile with 
                Encoding = FileEncoding.Base64
                Content = content }
        [<CustomOperation "owner">]
        member _.Owner (writeFile:WriteFile, owner:string) =
            { writeFile with Owner = owner }
        [<CustomOperation "permissions">]
        member _.Permissions (writeFile:WriteFile, permissions:string) =
            { writeFile with Permissions = FilePermissions.Parse(permissions).Value }
        member _.Permissions (writeFile:WriteFile, permissions:FilePermissions) =
            { writeFile with Permissions = permissions.Value }
        [<CustomOperation "append">]
        member _.Append (writeFile:WriteFile, append:bool) =
            { writeFile with Append = append }

    let writeFile = WriteFileBuilder ()

    type AptSourceConfig = 
        {
            Name : string
            Source : AptSource
        }
            static member Default = { Name = ""; Source = AptSource.Default }

    type AptSourceBuilder () =
        member _.Yield _ = AptSourceConfig.Default
        [<CustomOperation "name">]
        member _.Name (aptSource:AptSourceConfig, name:string) =
            { aptSource with Name = name }
        [<CustomOperation "keyid">]
        member _.KeyId (aptSource:AptSourceConfig, keyid:string) =
            { aptSource with Source = { aptSource.Source with Keyid = keyid } }
        [<CustomOperation "key">]
        member _.Key (aptSource:AptSourceConfig, key:string) =
            { aptSource with Source = { aptSource.Source with Key = key } }
        [<CustomOperation "keyserver">]
        member _.KeyServer (aptSource:AptSourceConfig, keyserver:string) =
            { aptSource with Source = { aptSource.Source with Keyserver = keyserver } }
        [<CustomOperation "source">]
        member _.Source (aptSource:AptSourceConfig, source:string) =
            { aptSource with Source = { aptSource.Source with Source = source } }

    let aptSource = AptSourceBuilder ()

    /// Builder for a CloudConfig record.
    type CloudConfigBuilder () =
        member _.Yield _ = CloudConfig.Default
        [<CustomOperation "write_files">]
        member _.WriteFiles (cloudConfig:CloudConfig, writeFiles: WriteFile seq) =
            { cloudConfig with WriteFiles = writeFiles }
        [<CustomOperation "add_apt_sources">]
        member _.Apt (cloudConfig:CloudConfig, aptSources: AptSourceConfig seq) =
            match cloudConfig.Apt with
            | Some apt ->
                for source in aptSources do
                    apt.Sources.[source.Name] <- source.Source
                cloudConfig
            | None ->
                let sources = aptSources |> Seq.map (fun s -> s.Name, s.Source) |> dict
                { cloudConfig with Apt = Some (Apt (Sources = sources)) }
        [<CustomOperation "package_update">]
        member _.PackageUpdate (cloudConfig:CloudConfig, packageUpdate:bool) =
            { cloudConfig with PackageUpdate = Some packageUpdate }
        [<CustomOperation "package_upgrade">]
        member _.PackageUpgrade (cloudConfig:CloudConfig, packageUpgrade:bool) =
            { cloudConfig with PackageUpgrade = Some packageUpgrade }
        [<CustomOperation "add_packages">]
        member _.AddPackages (cloudConfig:CloudConfig, packages:Package seq) =
            { cloudConfig with Packages = Seq.append cloudConfig.Packages packages }
        member _.AddPackages (cloudConfig:CloudConfig, packages:string seq) =
            let packages = packages |> Seq.map Package
            { cloudConfig with Packages = Seq.append cloudConfig.Packages packages }
        [<CustomOperation "final_message">]
        member _.FinalMessage (cloudConfig:CloudConfig, message:string) =
            { cloudConfig with FinalMessage = Some message }
        [<CustomOperation "run_commands">]
        member _.RunCommands (cloudConfig:CloudConfig, commands:string seq seq) =
            let cmdList = commands |> Seq.map List.ofSeq |> List.ofSeq
            { cloudConfig with
                RunCmd =
                    match cloudConfig.RunCmd with
                    | Some (RunCmd runCmd) -> List.append runCmd cmdList
                    | None -> cmdList
                    |> RunCmd |> Some
            }

    let cloudConfig = CloudConfigBuilder ()

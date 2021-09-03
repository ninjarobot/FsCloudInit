module ConfigGeneration

open System
open System.IO
open System.IO.Compression
open Expecto
open TestShared
open FsCloudInit

[<Tests>]
let tests =
    testList "cloud-config generation" [
        test "Generates empty cloud config" {
            CloudConfig.Default
            |> Writer.write
            |> matchExpectedAt "empty-config.yaml"
        }
        test "Generates basic cloud config" {
            {
                CloudConfig.Default with
                    PackageUpgrade = Some true
            }
            |> Writer.write
            |> matchExpectedAt "package-upgrade.yaml"
        }
        test "Generates with packages to install" {
            {
                CloudConfig.Default with
                    Packages = [ Package "httpd" ]
                    PackageUpgrade = Some true
            }
            |> Writer.write
            |> matchExpectedAt "package-install.yaml"
        }
        testAsync "Embed Microsoft apt source and key" {
            // curl -sSL https://packages.microsoft.com/config/ubuntu/18.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft-prod.list
            let! aptSourceRes = http.GetAsync "https://packages.microsoft.com/config/ubuntu/18.04/prod.list" |> Async.AwaitTask
            let! aptSource = aptSourceRes.Content.ReadAsStringAsync () |> Async.AwaitTask
            // curl -sSL https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
            let! gpgKeyRes = http.GetAsync "https://packages.microsoft.com/keys/microsoft.asc" |> Async.AwaitTask
            let! gpgKey = gpgKeyRes.Content.ReadAsStringAsync () |> Async.AwaitTask
            {
                CloudConfig.Default with
                    Apt =
                        Apt (
                            Sources =
                                dict  [
                                    "microsoft-prod", { AptSource.Default with Key = gpgKey; Source = aptSource}
                                ]
                        ) |> Some
                    PackageUpdate = Some true
                    Packages = [Package "apt-transport-https"; Package "dotnet-sdk-5.0"]
            }
            |> Writer.write
            |> matchExpectedAt "apt-source.yaml"
        }
        testAsync "Install specific dotnet SDK version" {
            let! aptSourceRes = http.GetAsync "https://packages.microsoft.com/config/ubuntu/18.04/prod.list" |> Async.AwaitTask
            let! aptSource = aptSourceRes.Content.ReadAsStringAsync () |> Async.AwaitTask
            let! gpgKeyRes = http.GetAsync "https://packages.microsoft.com/keys/microsoft.asc" |> Async.AwaitTask
            let! gpgKey = gpgKeyRes.Content.ReadAsStringAsync () |> Async.AwaitTask
            {
                CloudConfig.Default with
                    Apt =
                        Apt (
                            Sources =
                                dict [
                                    "microsoft-prod", { AptSource.Default with Key = gpgKey; Source = aptSource}
                                ]
                        ) |> Some
                    PackageUpdate = Some true
                    Packages = [
                        Package "apt-transport-https"
                        PackageVersion (PackageName="dotnet-sdk-5.0", PackageVersion="5.0.103-1")
                    ]
            }
            |> Writer.write
            |> matchExpectedAt "package-specific.yaml" 
        }
        test "Embed file" {
            let content = "hello world"
            {
                CloudConfig.Default with
                    WriteFiles = [
                        {
                            WriteFile.Default with
                                Encoding = FileEncoding.Base64
                                Content = content |> System.Text.Encoding.UTF8.GetBytes |> Convert.ToBase64String
                                Path = "/var/lib/data/hello"
                        }
                    ]
            }
            |> Writer.write
            |> matchExpectedAt "file-embedding.yaml"
        }
        test "Embed Gzipped file" {
            let contentStream = new MemoryStream("hello world" |> System.Text.Encoding.UTF8.GetBytes)
            use compressedContent = new MemoryStream()
            using (new GZipStream(compressedContent, CompressionMode.Compress))
                (fun gz -> contentStream.CopyTo(gz))
            let b64 = compressedContent.ToArray() |> Convert.ToBase64String
            {
                CloudConfig.Default with
                    WriteFiles = [
                        {
                            WriteFile.Default with
                                Encoding = FileEncoding.GzipBase64
                                Content = b64
                                Path = "/var/lib/data/hello"
                        }
                    ]
            }
            |> Writer.write
            |> matchExpectedAt "file-embedding-gzip.yaml"
        }
        test "File permission string generation" {
            let perms1 = {
                User = FilePermissions.RWX
                Group = FilePermissions.RW
                Others = FilePermissions.R
            }
            Expect.equal perms1.Value "0764" "Unexpected permission mask perms1"
            let perms2 = {
                User = FilePermissions.None
                Group = FilePermissions.None
                Others = FilePermissions.None
            }
            Expect.equal perms2.Value "0000" "Unexpected permission mask perms2"
            let perms3 = {
                User = FilePermissions.RW
                Group = FilePermissions.R
                Others = FilePermissions.R
            }
            Expect.equal perms3.Value "0644" "Unexpected permission mask perms3"
            let perms4 = {
                User = FilePermissions.R
                Group = FilePermissions.None
                Others = FilePermissions.None
            }
            Expect.equal perms4.Value "0400" "Unexpected permission mask perms4"
        }
        test "File permission string parsing" {
            let perms764 = FilePermissions.Parse "764"
            let expected = {
                User = FilePermissions.RWX
                Group = FilePermissions.RW
                Others = FilePermissions.R
            }
            Expect.equal perms764 expected "Parsing permissions returned incorrect value."
        }
        test "Embed readonly file" {
            let content = "hello world"
            {
                CloudConfig.Default with
                    WriteFiles = [
                        {
                            WriteFile.Default with
                                Encoding = FileEncoding.Base64
                                Content = content |> System.Text.Encoding.UTF8.GetBytes |> Convert.ToBase64String
                                Path = "/var/lib/data/hello"
                                Owner = "azureuser:azureuser"
                                Permissions = {
                                    User = FilePermissions.R
                                    Group = FilePermissions.None
                                    Others = FilePermissions.None }.Value
                        }
                    ]
            }
            |> Writer.write
            |> matchExpectedAt "file-embedding-readonly.yaml"
        }
        test "Run a command" {
            {
                CloudConfig.Default with
                    RunCmd =
                        [
                            [ "ls"; "-l"; "/" ]
                            [ "sh"; "-c"; "date >> whatsthetime.txt && cat whatsthetime.txt" ]
                            "apt update".Split null |> List.ofArray
                        ] |> RunCmd |> Some
            }
            |> Writer.write
            |> matchExpectedAt "run-command.yaml"
        }
        test "Print a final message" {
            {
                CloudConfig.Default with
                    FinalMessage = Some "#### Cloud-init is done! ####"
            }
            |> Writer.write
            |> matchExpectedAt "final-message.yaml"
        }
    ]

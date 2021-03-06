module Tests

open System
open FsCloudInit
open Expecto

let http = new System.Net.Http.HttpClient()

[<Tests>]
let tests =
    testList "cloud-config generation" [
        test "Generates empty cloud config" {
            CloudConfig.Default
            |> Writer.write
            |> Console.WriteLine
        }
        test "Generates basic cloud config" {
            {
                CloudConfig.Default with
                    PackageUpgrade = Nullable true
            }
            |> Writer.write
            |> Console.WriteLine
        }
        test "Generates with packages to install" {
            {
                CloudConfig.Default with
                    Packages = [ "httpd" ]
                    PackageUpgrade = Nullable true
            }
            |> Writer.write
            |> Console.WriteLine
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
                                (
                                    [
                                        "microsoft-prod", { AptSource.Default with Key = gpgKey; Source = aptSource}
                                    ] |> dict
                                )
                            )
                    PackageUpdate = Nullable true
                    Packages = ["apt-transport-https"; "dotnet-sdk-5.0"]
            }
            |> Writer.write
            |> Console.WriteLine
        }
        test "Embed file " {
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
            |> Console.WriteLine
        }
    ]

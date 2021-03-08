module Tests

open System
open System.IO
open FsCloudInit
open Expecto

let http = new System.Net.Http.HttpClient()

let matchExpectedAt (expectedContentFile:string) (generatedConfig:string) =
    let expected = File.ReadAllText $"TestContent/{expectedContentFile}"
    Expect.equal (generatedConfig.Trim()) (expected.Trim()) $"Did not match expected config at TestContent/{expectedContentFile}"

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
    ]

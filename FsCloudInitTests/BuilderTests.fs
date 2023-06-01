module BuilderTests

open System.Collections.Generic
open System.IO
open System.IO.Compression
open Expecto
open TestShared
open FsCloudInit
open FsCloudInit.Builders

[<Tests>]
let tests =
    testList "cloud-config generation" [
        test "Install packages by name" {
            cloudConfig {
                package_upgrade true
                add_packages [
                    "httpd"
                ]
            }
            |> Writer.write
            |> matchExpectedAt "package-install.yaml"
        }
        test "Embed file builder" {
            cloudConfig {
                write_files [
                    writeFile {
                        path "/var/lib/data/hello"
                        content "hello world"
                    }
                ]
            }
            |> Writer.write
            |> matchExpectedAt "file-embedding.yaml"
        }
        test "Embed gzipped file builder" {
            let yaml =
                cloudConfig {
                    write_files [
                        writeFile {
                            path "/var/lib/data/hello"
                            gzip_data "hello world"
                        }
                    ]
                }
                |> Writer.write
            let data:Dictionary<string, ResizeArray<Dictionary<string,string>>> =
                YamlDotNet.Serialization.Deserializer().Deserialize(yaml)
            let content = data["write_files"].[0]["content"]
            let gzContent = content |> System.Convert.FromBase64String
            use uncompressed = new MemoryStream()
            using (new GZipStream(new MemoryStream(gzContent), CompressionMode.Decompress))
                ( fun gz -> gz.CopyTo uncompressed )
            let helloWorld = uncompressed.ToArray() |> System.Text.Encoding.UTF8.GetString
            Expect.equal helloWorld "hello world" "Unzipped data didn't match"
        }
        test "Embed readonly file builder" {
            cloudConfig {
                write_files [
                    writeFile {
                        path "/var/lib/data/hello"
                        content "hello world"
                        owner "azureuser:azureuser"
                        permissions "400"
                    }
                ]
            }
            |> Writer.write
            |> matchExpectedAt "file-embedding-readonly.yaml"
        }
        test "Embed file with defer builder" {
            cloudConfig {
                write_files [
                    writeFile {
                        path "/var/lib/data/hello"
                        content "hello world"
                        owner "azureuser:azureuser"
                        defer true
                    }
                ]
            }
            |> Writer.write
            |> matchExpectedAt "file-embedding-defer.yaml"
        }
        testAsync "Install dotnet with aptSource builders" {
            let! aptSourceRes = http.GetAsync "https://packages.microsoft.com/config/ubuntu/20.04/prod.list" |> Async.AwaitTask
            let! aptSourceVal = aptSourceRes.Content.ReadAsStringAsync () |> Async.AwaitTask
            let! gpgKeyRes = http.GetAsync "https://packages.microsoft.com/keys/microsoft.asc" |> Async.AwaitTask
            let! gpgKey = gpgKeyRes.Content.ReadAsStringAsync () |> Async.AwaitTask
            cloudConfig {
                add_apt_sources [
                    aptSource {
                        name "microsoft-prod"
                        key gpgKey
                        source aptSourceVal
                    }
                ]
                package_update true
                add_packages [
                    Package "apt-transport-https"
                    PackageVersion (PackageName="dotnet-sdk-6.0", PackageVersion="6.0.100-1")
                ]
            }
            |> Writer.write
            |> matchExpectedAt "package-specific.yaml"             
        }
        test "Final message with cloudConfig builder" {
            cloudConfig {
                final_message "#### Cloud-init is done! ####"
            }
            |> Writer.write
            |> matchExpectedAt "final-message.yaml"
        }
        test "Run commands with cloudConfig builder" {
            cloudConfig {
                run_commands [
                    [ "ls"; "-l"; "/" ]
                    [ "sh"; "-c"; "date >> whatsthetime.txt && cat whatsthetime.txt" ]
                    "apt update".Split null
                ]
            }
            |> Writer.write
            |> matchExpectedAt "run-command.yaml"
        }
    ]
module BuilderTests

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
        testAsync "Install dotnet with aptSource builders" {
            let! aptSourceRes = http.GetAsync "https://packages.microsoft.com/config/ubuntu/18.04/prod.list" |> Async.AwaitTask
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
                    PackageVersion (PackageName="dotnet-sdk-5.0", PackageVersion="5.0.103-1")
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
namespace FsCloudInit

open System
open System.Collections.Generic

module FileEncoding =
     [<Literal>]
     let Base64 = "b64"

[<Flags>]
type FilePermission =
    | None = 0
    | Execute = 1
    | Write = 2
    | Read = 4

type FilePermissions = {
    User : FilePermission
    Group : FilePermission
    Others : FilePermission
}
    with
        static member None = FilePermission.None
        static member R = FilePermission.Read
        static member RW = FilePermission.Read ||| FilePermission.Write
        static member RX = FilePermission.Read ||| FilePermission.Execute
        static member RWX = FilePermission.Read ||| FilePermission.Write ||| FilePermission.Execute
        member this.Value =
            $"0{int this.User}{int this.Group}{int this.Others}"

type WriteFile =
    {
        Encoding : string
        Content : string
        Owner : string
        Path : string
        Permissions : string
        Append : bool
    }
    static member Default =
        {
            Encoding = null
            Content = null
            Owner = null
            Path = null
            Permissions = null
            Append = false
        }

type AptSource =
    {
        Keyid : string
        Key : string
        Keyserver : string
        Source : string
    }
    static member Default =
        {
            Keyid = null
            Key = null
            Keyserver = null
            Source = null
        }

type Apt() =
    member val Sources = Unchecked.defaultof<IDictionary<string, AptSource>> with get, set

type Package =
    | Package of string
    | PackageVersion of PackageName:string * PackageVersion:string
    member this.Model =
        match this with
        | Package p -> box p
        | PackageVersion (name, ver) -> [ name; ver] |> ResizeArray |> box

type Cmd = string list
type RunCmd =
    | RunCmd of Cmd list
    member this.Model : string seq seq =
        match this with
        | RunCmd (commands) ->
            commands |> Seq.map Seq.ofList

type CloudConfig =
    {
        Apt : Apt option
        FinalMessage : string option
        Packages : Package seq
        PackageUpdate : bool option
        PackageUpgrade : bool option
        PackageRebootIfRequired : bool option
        RunCmd : RunCmd option
        WriteFiles : WriteFile seq
    }
    static member Default =
        {
            Apt = None
            FinalMessage = None
            Packages = []
            PackageUpdate = None
            PackageUpgrade = None
            PackageRebootIfRequired = None
            RunCmd = None
            WriteFiles = []
        }
    member this.ConfigModel =
        {|
            Apt = this.Apt |> Option.defaultValue Unchecked.defaultof<Apt>
            FinalMessage = this.FinalMessage |> Option.toObj
            Packages =
                if this.Packages |> Seq.isEmpty then null
                else this.Packages |> Seq.map (fun p -> p.Model)
            PackageUpdate = this.PackageUpdate |> Option.toNullable
            PackageUpgrade = this.PackageUpgrade |> Option.toNullable
            Runcmd = this.RunCmd |> Option.map(fun runCmd -> runCmd.Model) |> Option.toObj
            WriteFiles =
                if this.WriteFiles |> Seq.isEmpty then null
                else this.WriteFiles
        |}
    
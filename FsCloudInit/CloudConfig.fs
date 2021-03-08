namespace FsCloudInit

open System
open System.Collections.Generic

module FileEncoding =
     [<Literal>]
     let Base64 = "b64"

type WriteFile =
    {
        Encoding : string
        Content : string
        Owner : string
        Path : string
    }
    static member Default =
        {
            Encoding = null
            Content = null
            Owner = null
            Path = null
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

type CloudConfig =
    {
        Apt : Apt option
        Packages : Package seq
        PackageUpdate : bool option
        PackageUpgrade : bool option
        PackageRebootIfRequired : bool option
        WriteFiles : WriteFile seq
    }
    static member Default =
        {
            Apt = None
            Packages = []
            PackageUpdate = None
            PackageUpgrade = None
            PackageRebootIfRequired = None
            WriteFiles = []
        }
    member this.ConfigModel =
        {|
            Apt = this.Apt |> Option.defaultValue Unchecked.defaultof<Apt>
            Packages =
                if this.Packages |> Seq.isEmpty then null
                else this.Packages |> Seq.map (fun p -> p.Model)
            PackageUpdate = this.PackageUpdate |> Option.toNullable
            PackageUpgrade = this.PackageUpgrade |> Option.toNullable
            WriteFiles =
                if this.WriteFiles |> Seq.isEmpty then null
                else this.WriteFiles
        |}
    
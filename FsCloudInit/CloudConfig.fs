namespace FsCloudInit

open System
open System.Collections.Generic

type CloudConfigSection =
    | Packages of string list
    | PackageUpdate of bool
    | PackageUpgrade of bool
    | PackageRebootIfRequired of bool

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

type CloudConfig =
    {
        Apt : Apt
        Packages : string seq
        PackageUpdate : Nullable<bool>
        PackageUpgrade : Nullable<bool>
        PackageRebootIfRequired : Nullable<bool>
        WriteFiles : WriteFile seq
    }
    static member Default =
        {
            Apt = Unchecked.defaultof<Apt>
            Packages = null
            PackageUpdate = Nullable()
            PackageUpgrade = Nullable()
            PackageRebootIfRequired = Nullable()
            WriteFiles = null
        }
    
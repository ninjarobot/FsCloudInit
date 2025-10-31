(*
Copyright 2021-2025 Dave Curylo

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit
persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*)
namespace FsCloudInit

open System
open System.Collections.Generic
open YamlDotNet.Serialization

module FileEncoding =
    [<Literal>]
    let Base64 = "b64"

    [<Literal>]
    let GzipBase64 = "gz+base64"

[<Flags>]
type PosixFilePerm =
    | None = 0
    | Execute = 1
    | Write = 2
    | Read = 4

type FilePermissions =
    { User: PosixFilePerm
      Group: PosixFilePerm
      Others: PosixFilePerm }

    static member None = PosixFilePerm.None
    static member R = PosixFilePerm.Read
    static member RW = PosixFilePerm.Read ||| PosixFilePerm.Write
    static member RX = PosixFilePerm.Read ||| PosixFilePerm.Execute
    static member RWX = PosixFilePerm.Read ||| PosixFilePerm.Write ||| PosixFilePerm.Execute
    member this.Value = $"0{int this.User}{int this.Group}{int this.Others}"

    static member Parse(s: string) =
        match UInt32.TryParse s with
        | true, num when num > 777u -> invalidArg "string" "Invalid values for permission flags."
        | true, num ->
            { User = enum<PosixFilePerm> (int (((num % 1000u) - (num % 100u)) / 100u))
              Group = enum<PosixFilePerm> (int (((num % 100u) - (num % 10u)) / 10u))
              Others = enum<PosixFilePerm> (int (((num % 10u) - (num % 1u)) / 1u)) }
        | false, _ -> invalidArg "string" "Malformed permission flags."

module Sudo =
    /// Defines sudo options as "ALL=(ALL) NOPASSWD:ALL"
    let AllPermsNoPasswd = "ALL=(ALL) NOPASSWD:ALL"

module internal Serialization =
    let serializableSeq sequence =
        if isNull sequence || Seq.isEmpty sequence then null else ResizeArray sequence

    let defaultIfTrue b = if b then Unchecked.defaultof<_> else b

type WriteFile =
    { Encoding: string
      Content: string
      Owner: string
      Path: string
      [<YamlMember(ScalarStyle = YamlDotNet.Core.ScalarStyle.SingleQuoted)>]
      Permissions: string
      Append: bool
      Defer: bool }

    static member Default =
        { Encoding = null
          Content = null
          Owner = null
          Path = null
          Permissions = null
          Append = false
          Defer = false }

type AptSource =
    { Keyid: string
      Key: string
      Keyserver: string
      Source: string }

    static member Default =
        { Keyid = null
          Key = null
          Keyserver = null
          Source = null }

type Apt() =
    member val Sources = Unchecked.defaultof<IDictionary<string, AptSource>> with get, set

type Package =
    | Package of string
    | PackageVersion of PackageName: string * PackageVersion: string

    member this.Model =
        match this with
        | Package p -> box p
        | PackageVersion(name, ver) -> [ name; ver ] |> ResizeArray |> box

type Cmd = string list

type RunCmd =
    | RunCmd of Cmd list

    member this.Model: string seq seq =
        match this with
        | RunCmd commands -> commands |> Seq.map Seq.ofList

type SnapCommand =
    | SnapCommand of Cmd
    | SnapCommandString of string

    member this.Model =
        match this with
        | SnapCommand cmd -> cmd |> ResizeArray |> box
        | SnapCommandString str -> box str

type SnapConfig =
    { Commands: SnapCommand seq
      Assertions: string seq }

    static member Default =
        { Commands = []
          Assertions = [] }

    [<YamlIgnore>]
    member this.Model =
        {| Commands =
            this.Commands
            |> Seq.map (fun cmd -> cmd.Model)
            |> Serialization.serializableSeq
           Assertions = Serialization.serializableSeq this.Assertions |}

type PowerState =
    { Delay: int
      Mode: string
      Message: string
      Timeout: Nullable<int>
      Condition: string }
    
    static member Default =
        { Delay = 0
          Mode = null
          Message = null
          Timeout = Nullable()
          Condition = null }

module PowerState =

    module Mode =
        [<Literal>]
        let PowerOff = "powerooff"
        [<Literal>]
        let Reboot = "reboot"
        [<Literal>]
        let Halt = "halt"


type UbuntuPro =
    { Token: string
      Enable: string seq
      EnableBeta: string seq }

    static member Default = { Token = null; Enable = []; EnableBeta = [] }

    [<YamlIgnore>]
    member this.Model =
        { Token = this.Token
          Enable = Serialization.serializableSeq this.Enable
          EnableBeta = Serialization.serializableSeq this.EnableBeta }

module UbuntuPro =
    module Services =
        [<Literal>]
        let CcEal = "cc-eal"
        [<Literal>]
        let Cis = "cis"
        [<Literal>]
        let EsmApps = "esm-apps"
        [<Literal>]
        let EsmInfra = "esm-infra"
        [<Literal>]
        let Fips = "fips"
        [<Literal>]
        let FipsPreview = "fips-preview"
        [<Literal>]
        let FipsUpdates = "fips-updates"
        [<Literal>]
        let Livepatch = "livepatch"

type User =
    { Name: string
      ExpiredDate: string
      Gecos: string
      Groups: string seq
      HomeDir: string
      Inactive: Nullable<int>
      LockPasswd: bool
      NoCreateHome: bool
      NoLogInit: bool
      NoUserGroup: bool
      CreateGroups: bool
      PrimaryGroup: string
      SelinuxUser: string
      Shell: string
      SshAuthorizedKeys: string seq
      SshImportId: string seq
      SshRedirectUser: bool
      System: bool
      Sudo: string
      Uid: Nullable<int> }

    static member Default =
        { Name = null
          ExpiredDate = null
          Gecos = null
          Groups = []
          HomeDir = null
          Inactive = Nullable()
          LockPasswd = true
          NoCreateHome = false
          NoLogInit = false
          NoUserGroup = false
          CreateGroups = true
          PrimaryGroup = null
          SelinuxUser = null
          Shell = null
          SshAuthorizedKeys = []
          SshImportId = []
          SshRedirectUser = false
          System = false
          Sudo = null
          Uid = Nullable() }

    [<YamlIgnore>]
    member this.Model =
        { this with
            CreateGroups = Serialization.defaultIfTrue this.CreateGroups
            Groups = Serialization.serializableSeq this.Groups
            LockPasswd = Serialization.defaultIfTrue this.LockPasswd
            SshAuthorizedKeys = Serialization.serializableSeq this.SshAuthorizedKeys
            SshImportId = Serialization.serializableSeq this.SshImportId }

type CloudConfig =
    { Apt: Apt option
      FinalMessage: string option
      Packages: Package seq
      PackageUpdate: bool option
      PackageUpgrade: bool option
      PackageRebootIfRequired: bool option
      PowerState: PowerState option
      RunCmd: RunCmd option
      Snap: SnapConfig option
      UbuntuPro: UbuntuPro option
      Users: User seq
      WriteFiles: WriteFile seq }

    static member Default =
        { Apt = None
          FinalMessage = None
          Packages = []
          PackageUpdate = None
          PackageUpgrade = None
          PackageRebootIfRequired = None
          PowerState = None
          RunCmd = None
          Snap = None
          UbuntuPro = None
          Users = []
          WriteFiles = [] }

    member this.ConfigModel =
        {| Apt = this.Apt |> Option.defaultValue Unchecked.defaultof<Apt>
           FinalMessage = this.FinalMessage |> Option.toObj
           Packages = this.Packages |> Seq.map (fun p -> p.Model) |> Serialization.serializableSeq
           PackageUpdate = this.PackageUpdate |> Option.toNullable
           PackageUpgrade = this.PackageUpgrade |> Option.toNullable
           PowerState = this.PowerState |> Option.defaultValue Unchecked.defaultof<PowerState>
           Runcmd = this.RunCmd |> Option.map (fun runCmd -> runCmd.Model) |> Option.toObj
           Snap = this.Snap |> Option.map (fun s -> s.Model) |> Option.defaultValue Unchecked.defaultof<_>
           UbuntuPro = this.UbuntuPro |> Option.map (fun u -> u.Model) |> Option.defaultValue Unchecked.defaultof<_>
           Users =
            let users =
                this.Users |> Seq.map (fun u -> box u.Model) |> Serialization.serializableSeq

            if not <| isNull users then // Include the default user created by the cloud platform
                users.Insert(0, "default")

            users
           WriteFiles = this.WriteFiles |> Serialization.serializableSeq |}

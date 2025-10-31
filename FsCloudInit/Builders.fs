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
open System.IO.Compression

module Builders =

    /// Builder for a WriteFile record.
    type WriteFileBuilder() =
        member _.Yield _ = WriteFile.Default

        [<CustomOperation "path">]
        member _.Path(writeFile: WriteFile, path: string) = { writeFile with Path = path }

        [<CustomOperation "content">]
        member _.Content(writeFile: WriteFile, content: string) =
            { writeFile with
                Encoding = FileEncoding.Base64
                Content = content |> System.Text.Encoding.UTF8.GetBytes |> Convert.ToBase64String }

        [<CustomOperation "gzip_data">]
        member _.GZipData(writeFile: WriteFile, contentStream: System.IO.Stream) =
            use compressed = new System.IO.MemoryStream()
            using (new GZipStream(compressed, CompressionMode.Compress)) contentStream.CopyTo
            let content = compressed.ToArray() |> Convert.ToBase64String

            { writeFile with
                Encoding = FileEncoding.GzipBase64
                Content = content }

        member this.GZipData(writeFile: WriteFile, content: byte[]) =
            use ms = new System.IO.MemoryStream(content)
            this.GZipData(writeFile, ms)

        member this.GZipData(writeFile: WriteFile, content: string) =
            use ms = new System.IO.MemoryStream(content |> System.Text.Encoding.UTF8.GetBytes)
            this.GZipData(writeFile, ms)

        [<CustomOperation "base64_encoded_content">]
        member _.Base64EncodedContent(writeFile: WriteFile, content: string) =
            { writeFile with
                Encoding = FileEncoding.Base64
                Content = content }

        [<CustomOperation "owner">]
        member _.Owner(writeFile: WriteFile, owner: string) = { writeFile with Owner = owner }

        [<CustomOperation "permissions">]
        member _.Permissions(writeFile: WriteFile, permissions: string) =
            { writeFile with
                Permissions = FilePermissions.Parse(permissions).Value }

        member _.Permissions(writeFile: WriteFile, permissions: FilePermissions) =
            { writeFile with
                Permissions = permissions.Value }

        [<CustomOperation "append">]
        member _.Append(writeFile: WriteFile, append: bool) = { writeFile with Append = append }

        [<CustomOperation "defer">]
        member _.Defer(writeFile: WriteFile, defer: bool) = { writeFile with Defer = defer }

    let writeFile = WriteFileBuilder()

    type AptSourceConfig =
        { Name: string
          Source: AptSource }

        static member Default =
            { Name = ""
              Source = AptSource.Default }

    type AptSourceBuilder() =
        member _.Yield _ = AptSourceConfig.Default

        [<CustomOperation "name">]
        member _.Name(aptSource: AptSourceConfig, name: string) = { aptSource with Name = name }

        [<CustomOperation "keyid">]
        member _.KeyId(aptSource: AptSourceConfig, keyid: string) =
            { aptSource with
                Source = { aptSource.Source with Keyid = keyid } }

        [<CustomOperation "key">]
        member _.Key(aptSource: AptSourceConfig, key: string) =
            { aptSource with
                Source = { aptSource.Source with Key = key } }

        [<CustomOperation "keyserver">]
        member _.KeyServer(aptSource: AptSourceConfig, keyserver: string) =
            { aptSource with
                Source =
                    { aptSource.Source with
                        Keyserver = keyserver } }

        [<CustomOperation "source">]
        member _.Source(aptSource: AptSourceConfig, source: string) =
            { aptSource with
                Source =
                    { aptSource.Source with
                        Source = source } }

    let aptSource = AptSourceBuilder()

    type PowerStateBuilder() =
        member _.Yield _ = PowerState.Default

        [<CustomOperation "delay">]
        member _.DelayStateChange(powerState, delay) = { powerState with Delay = delay }

        [<CustomOperation "mode">]
        member _.Mode(powerState, mode) = { powerState with Mode = mode }

        [<CustomOperation "message">]
        member _.Message(powerState, message) = { powerState with Message = message }

        [<CustomOperation "timeout">]
        member _.Timeout(powerState, timeout) = { powerState with Timeout = timeout }

        [<CustomOperation "condition">]
        member _.Condition(powerState, condition) = { powerState with Condition = condition }

    let powerState = PowerStateBuilder()

    type UbuntuProBuilder() =
        member _.Yield _ = UbuntuPro.Default

        [<CustomOperation "token">]
        member _.Token(ubuntuPro, token) = { ubuntuPro with Token = token }
        [<CustomOperation "enable">]
        member _.Enable(ubuntuPro, service) =
            { ubuntuPro with Enable = Set.add service (Set.ofSeq ubuntuPro.Enable) }
        member _.Enable(ubuntuPro, services) =
            { ubuntuPro with Enable = Set.union (Set.ofSeq services) (Set.ofSeq ubuntuPro.Enable) }
        [<CustomOperation "enable_beta">]
        member _.EnableBeta(ubuntuPro, service) =
            { ubuntuPro with EnableBeta = Set.add service (Set.ofSeq ubuntuPro.EnableBeta) }
        member _.EnableBeta(ubuntuPro, services) =
            { ubuntuPro with EnableBeta = Set.union (Set.ofSeq services) (Set.ofSeq ubuntuPro.EnableBeta) }

    let ubuntuAdvantage = UbuntuProBuilder()
    let ubuntuPro = ubuntuAdvantage
    
    /// Builder for a User.
    type UserBuilder() =
        member _.Yield _ = User.Default

        [<CustomOperation "name">]
        member _.Name(user: User, name: string) = { user with Name = name }

        [<CustomOperation "expiredate">]
        member _.ExpireDate(user: User, expireDate: DateTimeOffset) =
            { user with
                ExpiredDate = expireDate.ToString("yyyy-MM-dd") }

        [<CustomOperation "gecos">]
        member _.Gecos(user: User, gecos: string) = { user with Gecos = gecos }

        [<CustomOperation "groups">]
        member _.Groups(user: User, groups: string seq) = { user with Groups = groups }

        [<CustomOperation "homedir">]
        member _.HomeDir(user: User, homedir: string) = { user with HomeDir = homedir }

        [<CustomOperation "inactive">]
        member _.InactiveInDays(user: User, days: int) = { user with Inactive = Nullable(days) }

        [<CustomOperation "lock_passwd">]
        member _.LockPasswd(user: User, lockPasswd: bool) = { user with LockPasswd = lockPasswd }

        [<CustomOperation "no_create_home">]
        member _.NoCreateHome(user: User, noCreateHome: bool) =
            { user with
                NoCreateHome = noCreateHome }

        [<CustomOperation "no_log_init">]
        member _.NoLogInit(user: User, noLogInit: bool) = { user with NoLogInit = noLogInit }

        [<CustomOperation "no_user_group">]
        member _.NoUserGroup(user: User, noUserGroup: bool) = { user with NoUserGroup = noUserGroup }

        [<CustomOperation "create_groups">]
        member _.CreateGroups(user: User, createGroups: bool) =
            { user with
                CreateGroups = createGroups }

        [<CustomOperation "primary_group">]
        member _.PrimaryGroup(user: User, primaryGroup: string) =
            { user with
                PrimaryGroup = primaryGroup }

        [<CustomOperation "selinux_user">]
        member _.SelinuxUser(user: User, selinuxUser: string) = { user with SelinuxUser = selinuxUser }

        [<CustomOperation "shell">]
        member _.Shell(user: User, shell: string) = { user with Shell = shell }

        [<CustomOperation "ssh_authorized_keys">]
        member _.SshAuthorizedKeys(user: User, sshAuthorizedKeys: string seq) =
            { user with
                SshAuthorizedKeys = Seq.append user.SshAuthorizedKeys sshAuthorizedKeys }

        [<CustomOperation "ssh_import_id">]
        member _.SshImportId(user: User, sshImportIds: string seq) =
            { user with
                SshImportId = Seq.append user.SshImportId sshImportIds }

        [<CustomOperation "ssh_import_github_id">]
        member _.SshImportGitHubId(user: User, gitHubId: string) =
            { user with
                SshImportId = Seq.append user.SshImportId [ $"gh:{gitHubId}" ] }

        [<CustomOperation "ssh_redirect_user">]
        member _.SshRedirectUser(user: User, sshRedirectUser: bool) =
            { user with
                SshRedirectUser = sshRedirectUser }

        [<CustomOperation "system">]
        member _.System(user: User, system: bool) = { user with System = system }

        [<CustomOperation "sudo">]
        member _.Sudo(user: User, sudo: string) = { user with Sudo = sudo }

        [<CustomOperation "uid">]
        member _.Uid(user: User, uid: int) = { user with Uid = Nullable(uid) }

    let user = UserBuilder()

    /// Builder for a CloudConfig record.
    type CloudConfigBuilder() =
        member _.Yield _ = CloudConfig.Default

        [<CustomOperation "write_files">]
        member _.WriteFiles(cloudConfig: CloudConfig, writeFiles: WriteFile seq) =
            { cloudConfig with
                WriteFiles = writeFiles }

        [<CustomOperation "add_apt_sources">]
        member _.Apt(cloudConfig: CloudConfig, aptSources: AptSourceConfig seq) =
            match cloudConfig.Apt with
            | Some apt ->
                for source in aptSources do
                    apt.Sources[source.Name] <- source.Source

                cloudConfig
            | None ->
                let sources = aptSources |> Seq.map (fun s -> s.Name, s.Source) |> dict

                { cloudConfig with
                    Apt = Some(Apt(Sources = sources)) }

        [<CustomOperation "package_update">]
        member _.PackageUpdate(cloudConfig: CloudConfig, packageUpdate: bool) =
            { cloudConfig with
                PackageUpdate = Some packageUpdate }

        [<CustomOperation "package_upgrade">]
        member _.PackageUpgrade(cloudConfig: CloudConfig, packageUpgrade: bool) =
            { cloudConfig with
                PackageUpgrade = Some packageUpgrade }

        [<CustomOperation "add_packages">]
        member _.AddPackages(cloudConfig: CloudConfig, packages: Package seq) =
            { cloudConfig with
                Packages = Seq.append cloudConfig.Packages packages }

        member _.AddPackages(cloudConfig: CloudConfig, packages: string seq) =
            let packages = packages |> Seq.map Package

            { cloudConfig with
                Packages = Seq.append cloudConfig.Packages packages }

        [<CustomOperation "final_message">]
        member _.FinalMessage(cloudConfig: CloudConfig, message: string) =
            { cloudConfig with
                FinalMessage = Some message }

        [<CustomOperation "power_state">]
        member _.PowerState(cloudConfig: CloudConfig, powerState: PowerState) =
            { cloudConfig with PowerState = Some powerState }

        [<CustomOperation "run_commands">]
        member _.RunCommands(cloudConfig: CloudConfig, commands: string seq seq) =
            let cmdList = commands |> Seq.map List.ofSeq |> List.ofSeq

            { cloudConfig with
                RunCmd =
                    match cloudConfig.RunCmd with
                    | Some(RunCmd runCmd) -> List.append runCmd cmdList
                    | None -> cmdList
                    |> RunCmd
                    |> Some }

        [<CustomOperation "attach_ubuntu_pro">]
        member _.AttachUbuntuPro(cloudConfig: CloudConfig, ubuntuPro: UbuntuPro) =
            {
                cloudConfig with
                    UbuntuPro = Some ubuntuPro
            }

        [<CustomOperation "users">]
        member _.Users(cloudConfig: CloudConfig, users: User seq) =
            { cloudConfig with
                Users = Seq.append cloudConfig.Users users }

    let cloudConfig = CloudConfigBuilder()

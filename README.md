FsCloudInit
===========

Create [cloud-init](https://cloudinit.readthedocs.io) virtual machine configuration files in F#.

[![Build and Test](https://github.com/ninjarobot/FsCloudInit/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/ninjarobot/FsCloudInit/actions/workflows/build-and-test.yml)
[![FsCloudInit on Nuget](https://buildstats.info/nuget/FsCloudInit)](https://www.nuget.org/packages/FsCloudInit/)

### Examples

FsCloudInit includes configuration builders for many common tasks to simplify building the cloud-config file.

#### Builders or records

Builders can reduce some of the complexity by handling things like type casting and using sane defaults.

```f#
cloudConfig {
    package_upgrade true
    add_packages [
        "httpd"
    ]
}
|> Writer.write
```

It's also possible without the builder.

```f#
{
    CloudConfig.Default with
        Packages = [ Package "httpd" ]
        PackageUpgrade = Some true
}
|> Writer.write
```


#### Write files

Write some arbitrary data to a file. It will be base64 encoded automatically so there won't be any character escaping issues.

```f#
cloudConfig {
    write_files [
        writeFile {
            path "/var/lib/data/hello"
            content "hello world"
            owner "root:root"
            permissions "400"
        }
    ]
}
|> Writer.write
```

#### Install packages

```f#
    cloudConfig {
        package_upgrade
        add_packages [
            "curl"
            "screen"
            "httpd"
        ]
    }
    |> Writer.write
```

#### Run commands

```f#
cloudConfig {
    run_commands [
        [ "ls"; "-l"; "/" ]
        [ "sh"; "-c"; "date >> whatsthetime.txt && cat whatsthetime.txt" ]
        "apt update".Split null
    ]
}
|> Writer.write
```

#### Print a final message when done

```f#
cloudConfig {
    final_message "#### Cloud-init is done! ####"
}
|> Writer.write
```

#### Pull external data for the configuration

Installing the dotnet 5.0 SDK on a VM. This pulls the Microsoft package source and
signing key when building the cloud-init configuration.

```f#
async {
    // curl -sSL https://packages.microsoft.com/config/ubuntu/18.04/prod.list | sudo tee /etc/apt/sources.list.d/microsoft-prod.list
    let! aptSourceRes = http.GetAsync "https://packages.microsoft.com/config/ubuntu/18.04/prod.list" |> Async.AwaitTask
    let! aptSourceVal = aptSourceRes.Content.ReadAsStringAsync () |> Async.AwaitTask
    // curl -sSL https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
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
    |> Console.WriteLine
}
```

The above snippet writes a cloud-init configuration file to the console, resulting in
the following configuration file that can be used to install the SDK on a server:

```yaml
#cloud-config           
apt:
  sources:
    microsoft-prod:
      key: >
        -----BEGIN PGP PUBLIC KEY BLOCK-----

        Version: GnuPG v1.4.7 (GNU/Linux)


        mQENBFYxWIwBCADAKoZhZlJxGNGWzqV+1OG1xiQeoowKhssGAKvd+buXCGISZJwT

        LXZqIcIiLP7pqdcZWtE9bSc7yBY2MalDp9Liu0KekywQ6VVX1T72NPf5Ev6x6DLV

        7aVWsCzUAF+eb7DC9fPuFLEdxmOEYoPjzrQ7cCnSV4JQxAqhU4T6OjbvRazGl3ag

        OeizPXmRljMtUUttHQZnRhtlzkmwIrUivbfFPD+fEoHJ1+uIdfOzZX8/oKHKLe2j

        H632kvsNzJFlROVvGLYAk2WRcLu+RjjggixhwiB+Mu/A8Tf4V6b+YppS44q8EvVr

        M+QvY7LNSOffSO6Slsy9oisGTdfE39nC7pVRABEBAAG0N01pY3Jvc29mdCAoUmVs

        ZWFzZSBzaWduaW5nKSA8Z3Bnc2VjdXJpdHlAbWljcm9zb2Z0LmNvbT6JATUEEwEC

        AB8FAlYxWIwCGwMGCwkIBwMCBBUCCAMDFgIBAh4BAheAAAoJEOs+lK2+EinPGpsH

        /32vKy29Hg51H9dfFJMx0/a/F+5vKeCeVqimvyTM04C+XENNuSbYZ3eRPHGHFLqe

        MNGxsfb7C7ZxEeW7J/vSzRgHxm7ZvESisUYRFq2sgkJ+HFERNrqfci45bdhmrUsy

        7SWw9ybxdFOkuQoyKD3tBmiGfONQMlBaOMWdAsic965rvJsd5zYaZZFI1UwTkFXV

        KJt3bp3Ngn1vEYXwijGTa+FXz6GLHueJwF0I7ug34DgUkAFvAs8Hacr2DRYxL5RJ

        XdNgj4Jd2/g6T9InmWT0hASljur+dJnzNiNCkbn9KbX7J/qK1IbR8y560yRmFsU+

        NdCFTW7wY0Fb1fWJ+/KTsC4=

        =J6gs

        -----END PGP PUBLIC KEY BLOCK-----
      source: deb [arch=amd64] https://packages.microsoft.com/ubuntu/18.04/prod bionic main
packages:
- apt-transport-https
- dotnet-sdk-5.0
package_update: true
```

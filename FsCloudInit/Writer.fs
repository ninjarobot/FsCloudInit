namespace FsCloudInit

open System
open YamlDotNet.Serialization

module Writer =

    let write (config:CloudConfig) =
        let serializer =
            SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build()
        String.Concat ("#cloud-config", Environment.NewLine, serializer.Serialize config.ConfigModel)

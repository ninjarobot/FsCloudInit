module TestShared

open System.IO
open Expecto

let http = new System.Net.Http.HttpClient()

let matchExpectedAt (expectedContentFile:string) (generatedConfig:string) =
    let expected = File.ReadAllText $"TestContent/{expectedContentFile}"
    Expect.equal (generatedConfig.Trim()) (expected.Trim()) $"Did not match expected config at TestContent/{expectedContentFile}"


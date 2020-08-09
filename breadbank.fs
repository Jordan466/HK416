namespace HK416

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open System.Text.RegularExpressions
open DSharpPlus
open DSharpPlus.EventArgs
open DSharpPlus.Entities
open Contracts

module BreadBank =
    let (|BreadBank|_|) messages =
        let message = List.head messages
        //remove punctuation
        //ignore own messages
        match toLower message.Content with
        | ParseRegex "" _ -> Some("", message.Channel)
        | _ -> None

namespace HK416

open System
open System.Text.RegularExpressions
open DSharpPlus.Entities

module Contracts =
    let (>>=) m f = Option.bind f m

    type Message =
        { Content: string
          Author: DiscordUser
          MentionedUsers: DiscordUser list
          Channel: DiscordChannel }

    let content message = message.Content
    let auther message = message.Author
    let isBot (user: DiscordUser) = user.IsBot
    let toLower (s: String) = s.ToLower()

    let (|ParseRegex|_|) regex str =
        let m = Regex(regex).Match(str)
        match m.Success with
        | true -> Some m.Value
        | false -> None

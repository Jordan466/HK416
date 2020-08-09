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
    let private script =
        [ "welcome to the bread bank"
          "we sell bread"
          "we sell loafs"
          "we got bread on deck"
          "bread on the floor"
          "toasted"
          "roasted"
          "shut the fuck up"
          "listen i just need a bagguette and a brioche"
          "we dont have either of those"
          "you can get the gluten free white bread or the potato bread"
          "what the fuck is gluten"
          "take that shit out"
          "its gluten free"
          "i dont care if its free"
          "swear on your fucking yeezys"
          "if you wanna fight"
          "we gon fight"
          "you tryin be on world star"
          "what you gon record it"
          "ye"
          "I got my dollar store camera"
          "whats the fucking situation"
          "what the fuck do you want"
          "im the mother fucking manager"
          "at the bread store"
          "bread"
          "tell him to take the mf gluten out the bread"
          "Imma need you to shut that bullshit up chief"
          "we cant take shit out the bread"
          "why put it in in the first place"
          "I know yall smoking that pack"
          "we got crackers no gluten"
          "fuck crackers"
          "its gluten free you want the gluten or nah"
          "hell no"
          "you better take the gluten out that damn shit"
          "look we got whole wheat gluten free texas toast gluten free tortilla"
          "fuck all that"
          "what bitch ass country are yall from where they got this bullshit at"
          "florida"
          "i knew it"
          "look you can either take this yeast or im calling the police"
          "im going weast"
          "nah dont call the police i got a warrent"
          "honestly fuck yall"
          "i aint never seen nobody act like this over no bread"
          "what the fuck are you saying"
          "all im saying is"
          "fuck yalls bread fuck the gluten and fuck them crackers"
          "but the crackers dont have gluten"
          "ill take those"
          "ok that's gonna be 5"
          "nah fuck that i aint paying" ]

    let (|BreadBank|_|) messages =
        let message = List.head messages
        //remove punctuation
        //ignore own messages
        let line =
            (List.tryFind ((=) message.Content)) script

        match message.Author.IsBot, line with
        | _, line when (List.last >> Some) script = line -> None
        | false, Some line ->
            let lineIndex = List.findIndex ((=) line) script
            let nextLine = script.[lineIndex + 1]
            Some(nextLine, message.Channel)
        | _ -> None

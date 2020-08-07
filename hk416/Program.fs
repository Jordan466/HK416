// Learn more about F# at http://fsharp.org

open System
open Discord.WebSocket
open Discord.Commands
open System.Reflection
open System.Threading.Tasks
open Discord
open FSharp.Control.Tasks
open System.Collections.Generic
open System.Text.RegularExpressions

let (>>=) m f = Option.bind f m

let trim (m:String) = Some (m.Trim())

type Message = {
    Content: string
    Author: SocketUser
    MentionedUsers: IReadOnlyCollection<SocketUser>
}

let toMessage (m:SocketUserMessage) = {
    Content = m.Content
    Author = m.Author
    MentionedUsers = m.MentionedUsers
}

let content m = m.Content
let auther m = m.Author
let isBot (u:SocketUser) = u.IsBot
let toLower (s:String) = s.ToLower()

let (|ParseRegex|_|) regex str =
   let m = Regex(regex).Match(str)
   match m.Success with
   | true -> Some m.Value
   | false -> None

let sendMessage (channel:ISocketMessageChannel) msg = task {
    let! r = channel.SendMessageAsync(msg, false, null, null)
    return ()
}

let mutable messages = []
let mutable happyEmote : string option = None
let mutable shookEmote : string option = None
let mutable drinkEmote : string option = None

let (|Mentioned|_|) message = 
    match message with
    | ParseRegex "<@!739455874434859028>" _ -> Some ()
    | _ -> None

let (|RepeatAfterThree|_|) messages =
    match List.take 3 messages with
    | messages when List.exists (auther >> isBot) messages -> None
    | messages -> 
        match List.map content messages with
        | [one; two; three] when one = two && one = three -> Some one
        | _ -> None

let (|Commander|_|) messages =
    match (List.map content >> List.head >> toLower) messages with
    // | Mentioned -> Some "Commander. I am all you need." TODO: Ideally match only when its a ping and nothing else in the message
    | m when m = "416" -> Some "Commander. I am all you need."
    | m when m = "hk416" -> Some "Commander. I am all you need."
    | _ -> None

let (|HK4M|_|) messages = 
    match (List.map content >> List.head >> toLower) messages with
    | m when m = "hk4m" -> Some "HKM4? I have no need for such a name anymore!"
    | _ -> None

let (|Pat|_|) messages = 
    let message = (List.head >> content) messages
    match happyEmote, message, message with
    | Some emote, Mentioned, ParseRegex "k!pat .*" _ -> Some emote
    | _ -> None

let (|Kanpai|_|) messages =
    let message = (List.head >> content >> toLower) messages
    match drinkEmote, message with
    | Some emote, ParseRegex "kanpai" _ -> Some emote
    | _ -> None

let trySetHappyEmote message = 
    match happyEmote, message with
    | None, ParseRegex "<:happy416:\\d*>" emote -> happyEmote <- Some emote
    | _ -> ()

let trySetShookEmote message = 
    match shookEmote, message with
    | None, ParseRegex "<:shook416:\\d*>" emote -> shookEmote <- Some emote
    | _ -> ()

let trySetDrinkEmote message = 
    match shookEmote, message with
    | None, ParseRegex "<:drink416:\\d*>" emote -> drinkEmote <- Some emote
    | _ -> ()

let respond sendMessage = task {
    match messages with
    | Pat m -> do! sendMessage m
    | Kanpai m -> do! sendMessage m
    | Commander m -> do! sendMessage m
    | HK4M m -> do! sendMessage m
    | RepeatAfterThree m -> do! sendMessage m
    | _ -> return ()
}

let client = new DiscordSocketClient()
let commands = new CommandService()

let handleMessage (message:SocketMessage) = task {
    let argPos = 0
    match message with
    | :? SocketUserMessage as message ->
        match message with
        | message when isNull message -> return ()
        | _ ->
            // message.MentionedUsers
            messages <- (toMessage message) :: messages
            trySetHappyEmote message.Content
            trySetShookEmote message.Content
            trySetDrinkEmote message.Content
            printfn "%O" message
            do! respond (sendMessage message.Channel)
            return ()
    | _ -> return ()
}

[<EntryPoint>]
let main argv =
    let assembly = Assembly.GetExecutingAssembly()
    let t = task {
        let! moduleInfo = commands.AddModulesAsync(assembly, null)
        client.add_Log (fun m -> printfn "%O" m; Task.CompletedTask)
        client.add_MessageReceived (fun m -> handleMessage m |> ignore; Task.CompletedTask)
        do! client.LoginAsync(TokenType.Bot, argv.[0])
        do! client.StartAsync()
        do! Task.Delay(-1)
        return ()
    }
    t.Wait()
    0 // return an integer exit code

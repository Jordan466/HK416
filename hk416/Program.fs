// Learn more about F# at http://fsharp.org

open System
open Discord.WebSocket
open Discord.Commands
open System.Reflection
open System.Threading.Tasks
open Discord
open FSharp.Control.Tasks
open System.Collections.Generic

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
let hk416Id = 739455874434859028UL
let hk4126Mentioned (mentioned:IReadOnlyCollection<SocketUser>) = Seq.exists (fun (s:SocketUser) -> s.Id = hk416Id) mentioned

let sendMessage (channel:ISocketMessageChannel) msg = task {
    let! r = channel.SendMessageAsync(msg, false, null, null)
    return ()
}

let (|RepeatAfterThree|_|) messages =
    match List.take 3 messages with
    | messages when List.exists (auther >> isBot) messages -> None
    | messages -> 
        match List.map content messages with
        | [one; two; three] when one = two && one = three -> Some one
        | _ -> None

let (|Commander|_|) messages =
    match (List.map content >> List.head >> toLower) messages with
    | m when m = "416" -> Some "Commander. I am all you need."
    | m when m = "hk416" -> Some "Commander. I am all you need."
    | _ -> None

let (|HK4M|_|) messages = 
    match (List.map content >> List.head >> toLower) messages with
    | m when m = "hk4m" -> Some "HKM4? I have no need for such a name anymore!"
    | _ -> None

// let (|Pat|_|) messages = 
//     match List.head messages with
//     | m when m.Content = "k!pat" -> Some Emote.TryParse "\\:happy416:"
//         let emote = Emote.Parse "\\:happy416:"
        
//     | _ -> None

let mutable messages = []

let respond sendMessage = task {
    match messages with
    | Commander m -> do! sendMessage m
    | HK4M m -> do! sendMessage m
    // | Pat m -> do! sendMessage m
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
            printfn "lastMessage: %O" message
            printfn "Messages: %O" messages
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

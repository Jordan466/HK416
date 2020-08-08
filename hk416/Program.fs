open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open System.Text.RegularExpressions
open DSharpPlus
open DSharpPlus.EventArgs
open DSharpPlus.Entities

let (>>=) m f = Option.bind f m

type Message = {
    Content: string
    Author: DiscordUser
    MentionedUsers: DiscordUser list
    Channel: DiscordChannel
}

let content message = message.Content
let auther message = message.Author
let isBot (user:DiscordUser) = user.IsBot
let toLower (s:String) = s.ToLower()

let (|ParseRegex|_|) regex str =
   let m = Regex(regex).Match(str)
   match m.Success with
   | true -> Some m.Value
   | false -> None

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
    | [one; two; three] when
        (one.Content = two.Content && one.Content = three.Content)
        && (one.Channel = two.Channel && one.Channel = three.Channel) ->
        Some (three.Content, three.Channel)
    | _ -> None

let (|Commander|_|) messages =
    match List.head messages with
    // | Mentioned -> Some "Commander. I am all you need." TODO: Ideally match only when its a ping and nothing else in the message
    | m when toLower m.Content = "416" || toLower m.Content = "hk416" ->
        Some ("Commander. I am all you need.", m.Channel)
    | _ -> None

let (|HK4M|_|) messages = 
    match List.head messages with
    | m when toLower m.Content = "hk4m" -> Some ("HKM4? I have no need for such a name anymore!", m.Channel)
    | _ -> None

let (|Pat|_|) messages = 
    let message = List.head messages
    match happyEmote, message.Content, message.Content with
    | Some emote, Mentioned, ParseRegex "k!pat .*" _ -> Some (emote, message.Channel)
    | _ -> None

let (|Kanpai|_|) messages =
    let message = List.head messages
    match drinkEmote, toLower message.Content with
    | Some emote, ParseRegex "kanpai" _ -> Some (emote, message.Channel)
    | _ -> None

let (|GoodMorning|_|) messages =
    let message = List.head messages
    match message.Content, message.Content with
    | Mentioned, ParseRegex "good morning" _ -> Some ("Good morning, Commander. I won't lose to anyone today.", message.Channel)
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

let respond (client:DiscordClient) = task {
    let sendMessage message channel = task {
        let! m = client.SendMessageAsync(channel, message)
        return ()
    }
        
    match messages with
    | Pat (m, c) -> 
        do! Task.Delay(1000)
        do! sendMessage m c
    | Kanpai (m, c) -> do! sendMessage m c
    | Commander (m, c) -> do! sendMessage m c
    | HK4M (m, c) -> do! sendMessage m c
    | GoodMorning (m,c) -> do! sendMessage m c
    | RepeatAfterThree (m, c) -> do! sendMessage m c
    | _ -> return ()
}

let handleMessage (client:DiscordClient) (m:MessageCreateEventArgs) = task {
    let message = {
        Content = m.Message.Content
        Author = m.Author
        MentionedUsers = List.ofSeq m.MentionedUsers
        Channel = m.Channel
    }
    messages <- message :: messages
    trySetHappyEmote message.Content
    trySetShookEmote message.Content
    trySetDrinkEmote message.Content
    do! respond client
}

let greeting (client:DiscordClient) (g:GuildCreateEventArgs) = task {
    let general = Seq.tryFind (fun (c:DiscordChannel) -> c.Name = "general") g.Guild.Channels
    match general with
    | None -> return ()
    | Some channel -> 
        let! m = client.SendMessageAsync(channel, "Are you the commander? HK416. Please remember this name, this extraordinary name.")
        return ()
}

[<EntryPoint>]
let main argv =
    let config = DiscordConfiguration()
    config.set_Token argv.[0]
    config.set_TokenType TokenType.Bot

    let client = new DiscordClient(config)
    client.add_MessageCreated (fun m -> handleMessage client m |> ignore; Task.CompletedTask)
    client.add_GuildCreated (fun g -> greeting client g |> ignore; Task.CompletedTask)

    let t = task {
        do! client.ConnectAsync()
        printfn "Connected"
        do! Task.Delay(-1)
    }
    t.Wait()
    0 // return an integer exit code

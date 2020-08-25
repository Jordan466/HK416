open System
open System.Threading.Tasks
open FSharp.Control.Tasks.V2
open System.Text.RegularExpressions
open DSharpPlus
open DSharpPlus.EventArgs
open DSharpPlus.Entities
open HK416.Contracts
open HK416.BreadBank

let mutable messages = []
let mutable happyEmote : string option = None
let mutable shookEmote : string option = None
let mutable drinkEmote : string option = None
let furiousPat = "https://i.ibb.co/fHfJ0MW/furiouspat.gif"
let hk416Id = 739455874434859028uL

let (|Mentioned|_|) message = 
    match message with
    | ParseRegex "<@!739455874434859028>" _ -> Some ()
    | _ -> None

let mutable lastUsed : DateTimeOffset option = None
let (|RepeatAfterThree|_|) messages =
    //TODO: Dont throw exceptions when there are less than 3 messages
    try
        match lastUsed, List.take 3 messages with
        | _, messages when List.exists (auther >> isBot) messages -> None
        | last, [one; two; three] when
            (one.Content = two.Content && one.Content = three.Content)
            && (one.Channel = two.Channel && one.Channel = three.Channel)
            && (last < Some (DateTimeOffset.Now.AddSeconds(20.0)) || last = None) ->
                lastUsed <- Some DateTimeOffset.Now
                Some (three.Content, three.Channel)
        | _, _ -> None
    with
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

let (|Pet|_|) messages = 
    let message = List.head messages
    match happyEmote, message.Content, message.Content with
    | Some emote, Mentioned, ParseRegex "k!pat .*" _ -> Some (emote, message.Channel)
    | _ -> None

let (|Pat|_|) messages = 
    let message = List.head messages
    match message.Content, message.Content with
    | Mentioned, ParseRegex "pat" _ ->
        let mentionedUsers =
            List.filter (fun (u:DiscordUser) -> u.Id <> hk416Id) message.MentionedUsers
            |> List.map (fun (u:DiscordUser) -> u.Mention)
            |> List.distinct
            |> List.fold (fun state m -> state + m + " ") ""
        Some (mentionedUsers, furiousPat, message.Channel)
    | _ -> None

let (|Kanpai|_|) messages =
    let message = List.head messages
    match drinkEmote, toLower message.Content with
    | Some emote, ParseRegex "kanpai" _ -> Some (emote, message.Channel)
    | _ -> None

let (|GoodMorning|_|) messages =
    let message = List.head messages
    match message.Content, toLower message.Content with
    | Mentioned, ParseRegex "good morning" _ -> Some ("Good morning, Commander. I won't lose to anyone today", message.Channel)
    | _ -> None

let (|Genki|_|) messages =
    let message = List.head messages
    match message.Content, toLower message.Content with
    | Mentioned, ParseRegex "how are you" _ -> Some ("I am perfect", message.Channel)
    | _ -> None

let (|PatMe|_|) messages =
    let message = List.head messages
    match toLower message.Content with
    | ParseRegex "pat me" _ -> Some (message.Author.Mention, furiousPat, message.Channel)
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
    match drinkEmote, message with
    | None, ParseRegex "<:drink416:\\d*>" emote -> drinkEmote <- Some emote
    | _ -> ()

let respond (client:DiscordClient) = task {
    let sendMessage message channel = task {
        let! m = client.SendMessageAsync(channel, message)
        // printfn "%s: %s" channel.Name message
        return ()
    }

    let sendEmbeded message channel embed = task {
        let embed = DiscordEmbedBuilder(ImageUrl = embed).Build()
        let! m = client.SendMessageAsync(channel, message, embed = embed)
        return ()
    }
    match messages with
    | Pet (m, c) -> 
        do! Task.Delay(1000)
        do! sendMessage m c
    | PatMe (m, e, c) -> do! sendEmbeded m c e
    | Pat (m, e, c) -> do! sendEmbeded m c e
    | Kanpai (m, c) -> do! sendMessage m c
    | Commander (m, c) -> do! sendMessage m c
    | HK4M (m, c) -> do! sendMessage m c
    | GoodMorning (m,c) -> do! sendMessage m c
    | Genki (m,c) -> do! sendMessage m c
    //TODO: command to toggle bread bank and other settings
    // | Toggle (m,c) -> do! sendMessage m c
    // | BreadBank (m,c) -> do! sendMessage m c
    | RepeatAfterThree (m, c) -> do! sendMessage m c
    | _ -> return ()
}

let handleMessage (client:DiscordClient) (m:MessageCreateEventArgs) = task {
    try
        let message = {
            Content = m.Message.Content
            Author = m.Author
            MentionedUsers = List.ofSeq m.MentionedUsers
            Channel = m.Channel
        }
        printfn "[%O] %s %s %s: %s" m.Message.Timestamp m.Guild.Name m.Channel.Name message.Author.Username message.Content
        match List.length messages with
        | l when l >= 1000 -> messages <- message :: List.take 1000 messages
        | _ -> messages <- message :: messages
        trySetHappyEmote message.Content
        trySetShookEmote message.Content
        trySetDrinkEmote message.Content
        do! respond client
    with
    | e -> printfn "%O" e
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

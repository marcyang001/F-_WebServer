
module program1

//make an alias
type Agent<'T> = MailboxProcessor<'T>

//the loop repeatedly calls the Receive member of the agent to get the next message 
//an agent is created using Agent.Start
let agent = Agent.Start(fun agent -> async {
    while true do
        let! msg = agent.Receive()
        printfn "Hello %s" msg 

    })

//three things to understand F# agents 

// 1. messages are queued 
// 2. An agent runs on a single logical thread 
// 3. Agents are cheap

//agent.Post("Marc")


//implementing the chat room agent 

//define the message type 

type ChatMessage = 
    | SendMessage of string
    | GetContent of AsyncReplyChannel<string> // It carries a value of AsyncReplyChannel<string>, which represents a reply channel.


//implementing the agent 



#if INTERACTIVE
#r "System.Xml.Linq.dll"
#endif

open System.Xml.Linq

let chat = Agent.Start(fun agent -> 

    let rec loop elements = async { 
    
        //pick next message from the mailbox 
        let! msg = agent.Receive()
        match msg with 
        | SendMessage text ->
            // Add message to the list & continue 
            let element = XElement(XName.Get("li"), text)
            return! loop (element :: elements)

        | GetContent reply -> 
            // Generate HTML with messages
            let html = XElement (XName.Get("ul"), elements)
            // Send it back as the reply
            reply.Reply(html.ToString())
            return! loop elements }

    loop [])

//uses pattern natcgubg to handle the two possible message types... 

//When the agent receives SEndMessage, the funcction creates a new XElement, appends it to the front of the list, keeping all messages in the chat room, and then calls itself recursively using return! 
// the return! construct means that control is transferred to the called workflow, so the recursion doesnt keep any stack that would overflow after a number of iterations 

//when the agent receives the GetContent message, it gets the reply channel as an argumen. 
// to build a response string, it creates a new <ul> element containing all current messages, converts it to string and sends it to the caller using the Reply method. 

//Then, the function continues looping with the current list of messages 

//The snippet showed a simple but fully functional agent 

//test 

chat.Post(SendMessage "Welcome to F# chat")
chat.Post(SendMessage "second chat message...")
chat.PostAndReply(GetContent) |> ignore

printfn "enter here" 

System.Console.ReadLine() |> ignore

// How to create a new AsyncReplyChannel<string>? The answer is that the channel shouldn't be explicitly created by the user. 
//# PostAndReply 
// when using the PostAndReply method, the agent creates the channel and adds it to the message before sending it to the agent
// the method then blocks until the message is processed (and the agent sends back the room contents as string). 
// for testing purposes, the snippet uses a blocking method. 
module program2

#if INTERACTIVE
#r "System.Xml.Linq.dll"
#endif

open System.Xml.Linq

type Agent<'T> = MailboxProcessor<'T>


//the message type is used only in the implementation of the agent
//it doesnt nned to be visible to the callers of the library. For this reason, the type is marked as internal
type internal ChatMessage = 
    | GetContent of AsyncReplyChannel<string> 
    | SendMessage of string

//the type Chatroom is declared using implicit constructor syntax. if the chat room had any additional 
//parameters (for example, name), they could be specified in the parenthese following the type name. 
//the body of the type starts with private fields declared using let bindings such as agent. The initialization code is essentially a part of the 
//the constructor, so the agent will be started when a new instance of the object is created
type ChatRoom() = 
    let agent = Agent.Start(fun agent ->
    
        let rec loop elements = async {
            let! msg = agent.Receive() 
            match msg with 
            | SendMessage text ->
                let element = XElement(XName.Get("li"), text)
                return! loop  (elements @ [element])
            | GetContent reply -> 
                let html = XElement(XName.Get("ul"), elements)
                reply.Reply(html.ToString())
                return! loop elements } 
        loop [] )

    // Members exposing chat room functionality
    // the message that can be sent to an agent typically correspond to the operations that the agent can perform

    // when sending a message to the chat room, it isn't necessary to wait until the message is processed, so the class will expose 
    // only a single nonblocking version of the member that immediately returns. The operation for getting the content from the chat room may take 
    // some time to complete because the agent first needs to process all of the previous pending messages. 
    // to support nonblocking calls, the class exposes both synchronous and asynchronous versions

    member x.SendMessage(msg) = 
        agent.Post(SendMessage msg)

    member x.GetContent() = 
        agent.PostAndReply(GetContent)
    
    //returns an asynchronous computation that waits for a reply without blocking a thread 
    member x.AsyncGetContent(?timeout) =
        agent.PostAndAsyncReply(GetContent, ?timeout=timeout)
    
    //adds an overloaded asynchronous method for getting the chat rom content from C# 
    member x.GetContentAsync() =
        Async.StartAsTask(agent.PostAndAsyncReply(GetContent))

    member x.GetContentAsync(cancellationToken) =
        Async.StartAsTask 
            (agent.PostAndAsyncReply(GetContent), 
             cancellationToken = cancellationToken)
    


// Example that uses the chat room object from F#

let room = new ChatRoom()
async { 
  while true do
    do! Async.Sleep(10000)
    let! html = room.AsyncGetContent()
    printfn "%s" html } |> Async.Start

room.SendMessage("Hello world!")

System.Console.ReadLine() |> ignore


//this class shows how to turn the agent into a reuseable .Net class. To do that, the class included 
//members that expose the two operations of the chat room. 
//be exposed both as synchronous and asynchronous methods so that users of the object can write nonblocking code 
// the snippet looked at how to expose the asynchronous version using both Async<T> 



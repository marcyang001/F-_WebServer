module program4

//implementing a chat server

// when the agent receives an HTTP request, it starts a new asynchronous workflow to handle the request and then continues waiting 
//fo the next request 
//no need to worry about the race condition 

// multiple threads but thread safe --> implemented using Agent 

//handling requests

// the server checks for two special commands. When the client requests the /chat path, 
// server replies with the content of a chat room
// the /post path is used for sending new messages to the chat room. 
// when the client requests any other path, the server treats it as a request for a file from some special directory 
// because the chat application also consists of a simple HTML fille and CSS style sheet

// global declarations that are used when handling requests 

open System.IO
open System.Net

#if INTERACTIVE
#load @"program2"
#endif 

#if INTERACTIVE
#load @"program3"
#endif 



open program3
open program2

let room = new ChatRoom()
// path that contains static files and a simple dictionary for get HTTP content type 
// of a file using the file extension. 
let root = __SOURCE_DIRECTORY__ + "\\"
let contentTypes = dict [ ".css", "text/css"; ".html", "text/html"]


// implement a function that asynchronously handles an incoming HTTP request

let handleRequest (context: HttpListenerContext ) = async {
    
    match context.Request.Url.LocalPath with
    | "/post" ->
        //send message to the chat room 
        room.SendMessage(context.Request.InputString)
        context.Response.Reply("OK")
    | "/chat" ->
        //get messages from the chat room (asynchronously!)
        let! text = room.AsyncGetContent()
        context.Response.Reply(text)
    | s ->
        //Handle an ordinary file request 
        let file = root + (if s = "/" then "chat.html" else s)
        if File.Exists(file) then
            let ext = Path.GetExtension(file).ToLower()
            let typ = contentTypes.[ext]
            context.Response.Reply(typ, File.ReadAllBytes(file))
        else
            context.Response.Reply(sprintf "File not found: %s" file)}


printfn "Start server at 10.160.75.122:8081"
let url = "http://10.160.75.122:8081/"

let server = HttpAgent.Start(url, fun mbox -> async {
    while true do 
      let! ctx = mbox.Receive()
      ctx |> handleRequest |> Async.Start })


printfn "Enter here"
System.Console.ReadLine() |> ignore





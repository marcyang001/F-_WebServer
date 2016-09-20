module program3

//Developing an HTTP Server Agent 
// The F# library doesnt include any special support for using agents for creating web servers 

// the programing model based on agents fits this problem domain extremely well and it is possible to use rich .Net libraries to create an agent that behabes as an HTTP web server 

[<AutoOpen>]
module HttpExtensions = 
    type System.Net.HttpListener with 
        member x.AsyncGetContext() = 
            Async.FromBeginEnd(x.BeginGetContext, x.EndGetContext)


//implementing the agent type 
// the aim of this section is to create an HttpAgent type that behaves almost like the usual 
//Agent<'T>. 
//HttpAgent do not send messages to the agent explicitly. Instead, it starts listening on a specified port. 
// When a client connects to the server, the agent will generate a new message carrying the information about the connection 
// the code that uses the HttpAgent can specify how to react to the message (usually by sending a web page back) 

// the method takes a function representing the body of the agent as an argument 
//The function is is created for the buffering of incoming HTTP requests:

#if INTERACTIVE
#load @"program2"
#endif
open ``program2``       
open System.Net
open System.Threading

// HttpAgent that listens for HTTP requests and handles 
// them using the function provided to the Start method 



type HttpAgent private (url, f) as this = 
    let tokenSource = new CancellationTokenSource()
    let agent = Agent.Start((fun _ -> f this), tokenSource.Token)
    let server = async {
        use listener = new HttpListener()
        listener.Prefixes.Add(url)
       
        listener.Start()
        
        while true do
            let! context = listener.AsyncGetContext()
            agent.Post(context) 
        }

    do Async.Start(server, cancellationToken = tokenSource.Token)

    //Asynchronously waits for the next incoming HTTP request
    /// The method should only be used from the body of the agent 
    member x.Receive(?timeout) = agent.Receive(?timeout = timeout)

    /// Stops the HTTP server and releases the TCP connection 
    member x.Stop() = tokenSource.Cancel()


    //Starts new HTTP server on the specified URL. 
    //The specified function represents computation running inside the agent 
    static member Start(url, f) = 
        new HttpAgent(url, f)


open System.IO
open System.Text

[<AutoOpen>]
module HttpExtensions2 = 
    type System.Net.HttpListenerRequest with
        member request.InputString =
            use sr = new StreamReader(request.InputStream)
            sr.ReadToEnd()
   
    //reply method
    type System.Net.HttpListenerResponse with
        //converts a string a byte array using UTF8 encoding and then write the array to the OUtputStream    
        member response.Reply(s:string) = 
            let buffer = Encoding.UTF8.GetBytes(s)
            response.ContentLength64 <- int64 buffer.Length
            response.OutputStream.Write(buffer,0,buffer.Length)
            response.OutputStream.Close()
        // overload writes the bytes given as an argument to the output stream and it also sets the
        // ContentType of the response (e.g. "binary/image" for GIF files)
        member response.Reply(typ, buffer:byte[]) = 
            response.ContentLength64 <- int64 buffer.Length
            response.ContentType <- typ
            response.OutputStream.Write(buffer,0,buffer.Length)
            response.OutputStream.Close()


// the inputString is StreamReader, to read the content as a string and ensures that the StreamReader
// is properly disposed of using the use keyword. The two Reply methods are slightly more complex. 


// ----------------------------------------------------------------------------

// The following listing shows how to create an HTTP server that responds to 
// any incoming request with just "Hello world!" string.

printfn "Starting the server at 8082"
let url = "http://localhost:8082/"
let server = HttpAgent.Start(url, fun server -> async {
    while true do 
        let! ctx = server.Receive()
        ctx.Response.Reply("Hello world!") })


// the user needs to specify the url and a function as the body of the agent. 
// The body of the function can wait


System.Console.ReadLine() |> ignore


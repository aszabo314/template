namespace TurtleFsi

open System
 
open TurtleDomain

open Aardvark.Base
open Aardvark.UI

module Repl =
    
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open Microsoft.FSharp.Compiler.Interactive.Shell

    open System.IO
    open System.Text
        
    //let fsiStdOut = MVar.empty()
    //let fsiStdErr = MVar.empty()

    //let fsiResult = MVar.empty()
    let fsiInput = MVar.empty()

    let fsi inStream outStream errStream =
        let argv = [| @"C:\Program Files (x86)\Microsoft SDKs\F#\4.0\Framework\v4.0\FsiAnyCPU.exe" |]
        let allArgs = Array.append argv [|"--noninteractive"|]

        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
        FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream) 
        
    let evalCmds expr (session : FsiEvaluationSession) =
        try
            match session.EvalExpression expr with
            | Some value ->
                match value.ReflectionValue with
                | :? seq<TurtleDrawingMsg> as cmds -> cmds |> Seq.toList
                | _ -> []
            | None -> []
        with
        | e -> Log.warn "exn: %A" e; []

module Stuff =
    open System.Text
    open System.IO

    let sbOut = new StringBuilder()
    let sbErr = new StringBuilder()
    let inStream = new StringReader("")
    let outStream = new StringWriter(sbOut)
    let errStream = new StringWriter(sbErr)

    let fsiThread =
        
        let stuff = MVar.empty()

        let fsiSession = Repl.fsi inStream outStream errStream
        
        let mk =
            async {
                do! Async.SwitchToNewThread()
                
                while true do 
                    let expr = MVar.take Repl.fsiInput
                    
                    let result = Repl.evalCmds expr fsiSession
                    MVar.put stuff (sbOut.ToString(), sbErr.ToString(), result)    
                    //MVar.put Repl.fsiResult result
            }
            
        Async.Start mk
        
        let rec proc () =
            proclist {
                let! (o,e,res) = Proc.Await (MVar.takeAsync stuff)
                Log.warn "ERR: %s" e
                Log.warn "OUT: %s" o
                let cleane = String.replace "\r\n" "<br>" e
                let cleano = String.replace "\r\n" "<br>" o
                //let! res = Proc.Await (MVar.takeAsync Repl.fsiResult)
                yield FsiOut cleano
                yield FsiErr cleane
                yield CmdSequence res
                yield! proc()
            }
        
        proc()
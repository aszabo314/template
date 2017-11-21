namespace TurtleFsi

open System
 
open TurtleDomain
open Turtle

open Aardvark.Base
open Aardvark.UI

module Repl =
    
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open Microsoft.FSharp.Compiler.Interactive.Shell

    open System.IO
    open System.Text

    let fsiInput = MVar.empty()

    let expr =
        """
#r "Turtle.exe"
#r "Aardvark.Base.dll"

open System
open Aardvark.Base
open Turtle
open TurtleDomain
        """

    let fsi inStream outStream errStream =
        let argv = [| "FsiAnyCPU.exe" |]
        let allArgs = Array.append argv [|"--noninteractive"|]//"-r:Turtle.exe";"-r:Aardvark.Base.dll"|]

        let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
        let s = FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream) 
        s.EvalInteraction expr |> ignore
        s
        
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
            }
            
        Async.Start mk
        
        let rec proc () =
            proclist {
                let! (o,e,res) = Proc.Await (MVar.takeAsync stuff)
                Log.line "ERR: %s" e
                Log.line "OUT: %s" o
                
                let cleane = String.replace "\r\n" lineSeparator e
                let cleano = String.replace "\r\n" lineSeparator o
                do outStream.Flush()
                do errStream.Flush()
                yield FsiOut cleano
                yield FsiErr cleane
                do sbOut.Clear() |> ignore
                do sbErr.Clear() |> ignore

                yield CmdSequence res
                yield! proc()
            }
        
        proc()
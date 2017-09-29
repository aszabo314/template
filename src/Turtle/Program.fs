module MediaUI

open System
open System.Windows.Forms

open Aardvark.Base
open Aardvark.Application
open Aardvark.Application.WinForms
open Aardvark.UI

open Suave
open Suave.WebPart



open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Interactive.Shell

open System.IO
open System.Text
let test () =
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
                | :? seq<string> as cmds -> cmds |> Seq.toList
                | _ -> []
            | None -> []
        with
        | e -> Log.warn "exn: %A" e; []

    let sbOut = new StringBuilder()
    let sbErr = new StringBuilder()
    let inStream = new StringReader("")
    let outStream = new StringWriter(sbOut)
    let errStream = new StringWriter(sbErr)

    let fsisession = fsi inStream outStream errStream

    let res = evalCmds "let a = 1 \n[\"fufufu\"; \"hahah\"]" fsisession

    Log.line "Res: %A" res

    Log.warn "ERR: %A" (sbErr.ToString())

    Log.line "OUT: %A" (sbErr.ToString())

    let res = evalCmds "let a = 1 \n[\"fufufu2\"; \"hahah2\"]" fsisession

    Log.line "Res: %A" res

    Log.warn "ERR: %A" (sbErr.ToString())

    Log.line "OUT: %A" (sbErr.ToString())

    let res = evalCmds "asfadgasdgs; \"hahah3\"]" fsisession

    Log.line "Res: %A" res

    Log.warn "ERR: %A" (sbErr.ToString())

    Log.line "OUT: %A" (sbErr.ToString())

    let res = evalCmds "let a = 1 \n[\"fufufu4\"; \"hahah4\"]" fsisession

    Log.line "Res: %A" res

    Log.warn "ERR: %A" (sbErr.ToString())

    Log.line "OUT: %A" (sbErr.ToString())


let startMedia argv =
    //test()
    //System.Environment.Exit 0
    Xilium.CefGlue.ChromiumUtilities.unpackCef()
    Chromium.init argv
    Ag.initialize()
    Aardvark.Init()
    use app = new OpenGlApplication()
    let runtime = app.Runtime
    use form = new Form(Width = 1024, Height = 768)

    let app = TurtleApp.TurtleDrawingApp.app

    let instance = 
        app |> App.start

    WebPart.startServer 4321 [ 
        MutableApp.toWebPart runtime instance
        Suave.Files.browseHome
    ]  

    use ctrl = new AardvarkCefBrowser()
    ctrl.Dock <- DockStyle.Fill
    form.Controls.Add ctrl
    ctrl.StartUrl <- "http://localhost:4321/"

    Application.Run form
    System.Environment.Exit 0

[<EntryPoint;STAThread>]
let main argv = startMedia argv; 0
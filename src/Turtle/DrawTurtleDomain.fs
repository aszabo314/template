namespace TurtleDomain

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.UI.Primitives

open Turtle

type Line =
    {
        Geometry : Line3d
        Color    : C4f
        Thickness: float
    }
    
[<DomainType>]
type TurtleDrawingModel =
    {
        Turtle : Turtle

        Lines : plist<Line>

        Camera : CameraControllerState

        FsiString : string
        FsiOut : string
        FsiErr : string
    }

type TurtleDrawingMsg =
    | Step
    | Pitch         of float
    | Yaw           of float
    | Roll          of float
    | Teleport      of V3d
    | Speed         of float
    | AddSpeed      of float
    | MultiplySpeed of float
    | MapSpeed      of (float -> float)

    | Draw          of bool
    | Color         of C4f
    | Thickness     of float

    | Reset

    | CameraMsg   of CameraController.Message

    | CmdSequence of list<TurtleDrawingMsg>
    | FsiString   of string
    | EvalFsi
    | FsiOut      of string
    | FsiErr      of string

module TurtleDrawing =
    
    let initialTurtle =
        Turtle.lookAtSimple V3d.OOO V3d.IOO V3d.OOI
    
    let initial =
        {
            Turtle = initialTurtle
            Lines = PList.empty
            Camera = { CameraController.initial with view = CameraView.lookAt (V3d.OII * 6.0) V3d.OOO V3d.OOI }
            FsiString = ""
            FsiOut = ""
            FsiErr = ""
        }

    let pitchTurtle angle m =
        { m with Turtle = m.Turtle |> Turtle.pitch angle }
    
    let yawTurtle angle m =
        { m with Turtle = m.Turtle |> Turtle.yaw angle }

    let rollTurtle angle m =
        { m with Turtle = m.Turtle |> Turtle.roll angle }

    let teleportTurtle point m =
        { m with Turtle = m.Turtle |> Turtle.teleport point }
    
    let setSpeed s m =
        { m with Turtle = { m.Turtle with Speed = s } }
    
    let setColor c m =
        { m with Turtle = { m.Turtle with Color = c } }
    
    let setThickness t m =
        { m with Turtle = { m.Turtle with Thickness = t } }

    let setDrawing d m =
        { m with Turtle = { m.Turtle with Drawing = d } }
    
    let reset m =
        {
            m with
                Turtle = initial.Turtle
                Lines  = initial.Lines
        }
    
    let step m =
        let oldTurtle = m.Turtle
        let newTurtle = m.Turtle |> Turtle.step
        let newLines =
            if m.Turtle.Drawing then
                let line = 
                    { 
                        Geometry = Line3d(oldTurtle.Position, newTurtle.Position)
                        Color    = m.Turtle.Color
                        Thickness= m.Turtle.Thickness
                    }
                
                m.Lines |> PList.append line
            else
                m.Lines
        {
            m with
                Turtle = newTurtle
                Lines = newLines
        }

    let cameraMsg msg m =
        {
            m with Camera = CameraController.update m.Camera msg
        }

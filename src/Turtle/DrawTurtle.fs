﻿namespace TurtleApp

open System
open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.SceneGraph

open Turtle
open TurtleDomain

module Shader =
    open FShade
        
    let hi = 200.0 / 255.0
    let lo = 172.0 / 255.0

    let qf = (V4d(hi,lo,lo,1.0))
    let qb = (V4d(lo,hi,hi,1.0))
    let ql = (V4d(lo,hi,lo,1.0))
    let qr = (V4d(hi,lo,hi,1.0))
    let qu = (V4d(lo,lo,hi,1.0))
    let qd = (V4d(hi,hi,lo,1.0))

    type Vertex =
        {
            [<Position>] p : V4d
            [<Semantic("ViewDirection")>] dir : V3d
        }

    let backgroundCubeVertex (v : Vertex) =
        vertex {
            let wp = uniform.ViewProjTrafoInv * v.p
            let wp = wp.XYZ / wp.W
            return 
                {
                    p = v.p
                    dir = wp.XYZ - uniform.CameraLocation |> Vec.normalize
                }
        }

    let backgroundCube (v : Vertex) =
        fragment {
            let dir = v.dir |> Vec.normalize

            let absDir = V3d(abs dir.X, abs dir.Y, abs dir.Z)

            if absDir.X > absDir.Y && absDir.X > absDir.Z then 
                if dir.X > 0.0 then return qf
                else return qb
            elif absDir.Y > absDir.X && absDir.Y > absDir.Z then
                if dir.Y > 0.0 then return ql
                else return qr
            else
                if dir.Z > 0.0 then return qu
                else return qd
        }

module TurtleDrawingApp =
    open Aardvark.Base.Incremental

    let view (mm : MTurtleDrawingModel) : DomNode<TurtleDrawingMsg> =
        
        let turtleSg =
            let center =
                IndexedGeometryPrimitives.solidSubdivisionSphere 
                    (Sphere3d.FromCenterAndRadius(V3d.OOO, 0.25))
                    4
                    C4b.Red
                |> Sg.ofIndexedGeometry
                |> Sg.shader {
                        do! DefaultSurfaces.trafo
                        do! DefaultSurfaces.vertexColor
                    }
            
            let lines =
                let c = V3d.OOO
                let f = V3d.OIO
                let u = V3d.OOI
                let r = V3d.IOO

                let lo = C4f(0.7, 0.7, 0.7, 1.0).ToC4b() 
                let hi = C4f(1.0, 0.7, 0.7, 1.0).ToC4b() 

                let verts = [| c;f; c;u; c;r; |]
                let cols = [| hi;hi;lo;lo;lo;lo|]
                
                Sg.render IndexedGeometryMode.LineList (DrawCallInfo( FaceVertexCount = 6, InstanceCount = 1 ))
                    |> Sg.vertexAttribute' DefaultSemantic.Positions verts
                    |> Sg.vertexAttribute' DefaultSemantic.Colors cols
                    |> Sg.shader {
                        do! DefaultSurfaces.trafo
                        do! DefaultSurfaces.thickLine
                        do! DefaultSurfaces.vertexColor
                    }
                    |> Sg.uniform "LineWidth" (Mod.constant 5.0)
                    |> Sg.trafo (
                        adaptive {
                            let! r = mm.Turtle.Right
                            let! f = mm.Turtle.Forward
                            let! u = mm.Turtle.Up
                            return Trafo3d.FromBasis (r, f, u, V3d.OOO)
                        })

            [center;lines]
                |> Sg.ofList
                |> Sg.trafo (mm.Turtle.Position |> Mod.map Trafo3d.Translation )
        
        let linesSg =
            let lineToSg (l : Line) =
                let center = l.Geometry.P0
                let ll = (l.Geometry.P1 - l.Geometry.P0)
                let axis = ll.Normalized
                let height = ll.Length

                IndexedGeometryPrimitives.solidCylinder 
                    center 
                    axis
                    height
                    l.Thickness
                    l.Thickness
                    8
                    (l.Color.ToC4b())
                    |> Sg.ofIndexedGeometry
            
            mm.Lines |> AList.toASet
                     |> ASet.map lineToSg
                     |> Sg.set
                     |> Sg.shader {
                            do! DefaultSurfaces.trafo
                            do! DefaultSurfaces.vertexColor
                            do! DefaultSurfaces.simpleLighting
                        }
        
        let backgroundStuff =
            let cross = IndexedGeometryPrimitives.coordinateCross V3d.III
                        |> Aardvark.SceneGraph.SgFSharp.Sg.ofIndexedGeometry

            let box =
                Sg.farPlaneQuad
                    |> Sg.shader {
                        do! Shader.backgroundCubeVertex
                        do! Shader.backgroundCube
                    }

            [cross; box]
                |> Sg.ofList
                |> Sg.shader {
                    do! DefaultSurfaces.trafo
                    do! DefaultSurfaces.vertexColor
                }

        let sg = 
            [backgroundStuff; linesSg; turtleSg]
                |> Sg.ofList
                |> Sg.noEvents

        let frustum = Frustum.perspective 90.0 0.1 100.0 1.0 |> Mod.constant
        let attributes = AttributeMap.ofList [ attribute "style" "width:100%; height: 100%"; attribute "data-samples" "8"]
        CameraController.controlledControl mm.Camera CameraMsg frustum attributes sg
        

    let update (m : TurtleDrawingModel) (msg : TurtleDrawingMsg) =
        match msg with
        | Step -> TurtleDrawing.step m
        | Pitch a -> TurtleDrawing.pitchTurtle a m
        | Yaw a -> TurtleDrawing.yawTurtle a m
        | Roll a -> TurtleDrawing.rollTurtle a m
        | Teleport p -> TurtleDrawing.teleportTurtle p m
        | Speed s -> TurtleDrawing.setSpeed s m

        | GoFasterBy s -> TurtleDrawing.setSpeed (m.Turtle.Speed + s) m
        | GoSlowerBy s -> TurtleDrawing.setSpeed (m.Turtle.Speed - s) m

        | Draw d -> TurtleDrawing.setDrawing d m
        | Color c -> TurtleDrawing.setColor c m
        | Thickness t -> TurtleDrawing.setThickness t m

        | Reset -> TurtleDrawing.reset m

        | CameraMsg cm -> TurtleDrawing.cameraMsg cm m

    let threads (m : TurtleDrawingModel)= 
        CameraController.threads m.Camera |> ThreadPool.map CameraMsg

    let doIt1 =
        [
            Step
            Step
            Pitch -Constant.PiHalf
            Step
            Color (C4f(0.9, 0.2, 0.3,1.0))
            Step
            Yaw  1.0
            Step
            Color (C4f(0.1, 0.2, 0.9,1.0))
            Speed 4.0
            Step
            Yaw 1.0
            Step
        ]

    let doIt =
        let maxIter = 1000
        let angleStep = (5.0 / float maxIter) * Constant.PiTimesTwo
        let speedStep = (1.0 / float maxIter)
        [
            yield Speed 1.0
            yield Color ((rgb2hsv 0.0 1.0 0.5).ToC4f())
            for i in 0 .. maxIter-1 do
                yield Step
                yield Yaw angleStep
                yield Roll angleStep
                yield GoSlowerBy speedStep
                yield Color ((rgb2hsv (float i / float maxIter) 1.0 0.5 ).ToC4f())
        ]

    let rec updateMany (msgs : list<TurtleDrawingMsg>) m =
        match msgs with
        | [] -> m
        | msg :: rest -> 
            updateMany rest (update m msg)

    let app =
        {
            unpersist = Unpersist.instance
            threads = threads
            initial = updateMany doIt TurtleDrawing.initial
            update = update
            view = view
        }

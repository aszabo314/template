namespace Turtle

open System

open Aardvark.Base
open Aardvark.Base.Incremental

[<DomainType>]
type Turtle =
    {
        Position : V3d
        Forward  : V3d
        Right    : V3d
        Up       : V3d

        Speed    : float
        Color    : C4f
        Thickness: float

        Drawing  : bool
    }

module Turtle =
    let lookAt (head : V3d) (target : V3d) (sky : V3d) speed color thickness drawing =
        let fw = target - head       |> Vec.normalize
        let r = Vec.cross fw sky     |> Vec.normalize
        let u = Vec.cross r fw       |> Vec.normalize

        {
            Position = head
            Forward  = fw
            Right    = r
            Up       = u

            Speed    = speed
            Color    = color
            Thickness= thickness

            Drawing  = drawing

        }

    let lookAtSimple head target sky =
        lookAt head target sky 1.0 C4f.DarkBlue 0.1 true

    let step turtle =
        { turtle with Position = turtle.Position + turtle.Forward * turtle.Speed }

    let rotate (rot : M44d) turtle =
        let r =     rot.TransformDir turtle.Right
        let u =     rot.TransformDir turtle.Up
        let fw =    rot.TransformDir turtle.Forward

        { turtle with Right = r; Up = u; Forward = fw }

    let pitch angle turtle =
        turtle |> rotate (M44d.RotationX (Constant.RadiansPerDegree * angle))

    let yaw angle turtle =
        turtle |> rotate (M44d.RotationZ (Constant.RadiansPerDegree * angle))

    let roll angle turtle =
        turtle |> rotate (M44d.RotationY (Constant.RadiansPerDegree * angle))

    let teleport pos turtle =
        { turtle with Position = pos }
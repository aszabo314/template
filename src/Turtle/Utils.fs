namespace Turtle

open System
open Aardvark.Base

[<AutoOpen>]
module Utils =

    let lineSeparator = "((+))"

    let hsv2rgb (h : float) (s : float) (v : float) =
        let s = clamp 0.0 1.0 s
        let v = clamp 0.0 1.0 v

        let h = h % 1.0
        let h = if h < 0.0 then h + 1.0 else h
        let hi = floor ( h * 6.0 ) |> int
        let f = h * 6.0 - float hi
        let p = v * (1.0 - s)
        let q = v * (1.0 - s * f)
        let t = v * (1.0 - s * ( 1.0 - f ))
        match hi with
            | 1 -> V3d(q,v,p)
            | 2 -> V3d(p,v,t)
            | 3 -> V3d(p,q,v)
            | 4 -> V3d(t,p,v)
            | 5 -> V3d(v,p,q)
            | _ -> V3d(v,t,p)

    let rgb2hsv (r : float) (g : float) (b : float) =
        let r = int (r * 255.0)
        let g = int (g * 255.0)
        let b = int (b * 255.0)

        let ma = max r (max g b)
        let mi = min r (min g b)

        let color = System.Drawing.Color.FromArgb(255,r,g,b)
        let h = (float (color.GetHue())) / 360.0
        let s = if ma = 0 then 0.0 else 1.0 - (float mi / float ma)
        let v = float ma / 255.0;

        V3d(h,s,v)


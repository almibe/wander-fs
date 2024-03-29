﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

module Ligature.ZMQ.Main

open System.IO
open Ligature.Sqlite.Main
open Ligature.ZMQ.Config
open Ligature
open Ligature.Bend.Main
open NetMQ.Sockets
open NetMQ
open System
open Ligature.Bend.Lib.Preludes

let rec serve (server: ResponseSocket) (instance: ILigature) =
    let script = server.ReceiveFrameString()
    let res = run script (instancePrelude instance)
    server.SendFrame(printResult res)
    serve server instance

[<EntryPoint>]
let main _ =
    Console.WriteLine("Starting Ligature ZeroMQ.")
    let config = readConfig ()
    let instance =
        match config.persistance with
        | Sqlite config -> ligatureSqlite config
    use server = new ResponseSocket()
    server.Bind("tcp://localhost:4200")
    Console.WriteLine("Started on port 4200.")
    serve server instance
    0

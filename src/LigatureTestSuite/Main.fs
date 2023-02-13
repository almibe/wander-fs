﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

module Ligature.TestSuite
open Expecto
open Ligature

let inline todo<'T> : 'T = raise (System.NotImplementedException("todo"))

/// Unsafe helper function for creating Datasets.
let ds name =
    match dataset name with
    | Ok (ds) -> ds
    | Error (_) -> todo

/// Unsafe helper function for creating Identifiers.
let id ident =
    match identifier ident with
    | Ok (i) -> i
    | Error (_) -> todo

/// Unsafe helper function for creating statements for testing.
let statement (entity: string) (attribute: string) (value: Value) =
    let e = id entity
    let a = id attribute
    { Entity = e; Attribute = a; Value = value; }

let ligatureTestSuite (createInstance: Unit -> Ligature) =
    let helloDS = ds "hello"
    let hello2DS = ds "hello2"
    let hello3DS = ds "hello3"

    let jvName = statement "character:1" "name" (String "Jean Valjean")
    let jvNumber = statement "character:1" "prisonerNumber" (Integer 24601)
    let javertName = statement "character:2" "name" (String "Inspector Javert")
    let nemesis = statement "character:2" "hasNemesis" (id "character:1" |> Identifier) 
    let statements = Array.sort [|jvName; jvNumber; javertName; nemesis|]

    testList "Ligature Test Suite" [
        testCase "start with no Datasets" <| fun _ ->
            let instance = createInstance ()
            let datasets = instance.AllDatasets()
            Expect.equal (datasets) (Ok Array.empty) "Datasets should be empty."
        testCase "add a Dataset" <| fun _ ->
            let instance = createInstance ()
            instance.CreateDataset (helloDS) |> ignore
            let datasets = instance.AllDatasets()
            Expect.equal (datasets) (Ok [|helloDS|]) "Dataset should contain hello Dataset."
        testCase "check if Dataset exists" <| fun _ ->
            let instance = createInstance ()
            Expect.equal (instance.DatasetExists helloDS) (Ok false) "Dataset shouldn't exist before adding."
            instance.CreateDataset (helloDS) |> ignore
            Expect.equal (instance.DatasetExists helloDS) (Ok true) "Dataset should now exist."
        //TODO - add "match datasets prefix exact"
        //TODO - add "match datasets prefix"
        //TODO - add "match datasets range"
        testCase "create and remove new Dataset" <| fun _ ->
            let instance = createInstance ()
            instance.CreateDataset helloDS |> ignore
            instance.CreateDataset hello2DS |> ignore
            instance.RemoveDataset helloDS |> ignore
            instance.RemoveDataset hello3DS |> ignore
            let datasets = instance.AllDatasets ()
            Expect.equal datasets (Ok [|hello2DS|]) "Dataset should only contain hello2 Dataset."

        testCase "new Dataset should be empty" <| fun _ ->
            let instance = createInstance ()
            instance.CreateDataset (helloDS) |> ignore
            let result = instance.Query helloDS (fun tx -> tx.AllStatements ())
            Expect.equal result (Ok [||]) "Newly created Dataset should be empty."
        testCase "add new Statements to Dataset" <| fun _ ->
            let instance = createInstance ()
            instance.CreateDataset (helloDS) |> ignore
            instance.Write helloDS (fun tx -> Array.iter (fun statement -> tx.AddStatement statement |> ignore) statements; Ok ()) |> ignore
            let result = instance.Query helloDS (fun tx -> tx.AllStatements ())
            let result = Result.map (fun statements -> Array.sort statements) result
            Expect.equal result (Ok statements) "Dataset should contain new Statements."
        testCase "add new Statements to Dataset with dupes" <| fun _ ->
            let instance = createInstance ()
            instance.CreateDataset (helloDS) |> ignore
            instance.Write helloDS (fun tx ->
                Array.iter (fun statement -> tx.AddStatement statement |> ignore) statements
                Array.iter (fun statement -> tx.AddStatement statement |> ignore) statements
                Array.iter (fun statement -> tx.AddStatement statement |> ignore) statements
                Ok ()) |> ignore
            let result = instance.Query helloDS (fun tx -> tx.AllStatements ())
            let result = Result.map (fun statements -> Array.sort statements) result
            Expect.equal result (Ok statements) "Dataset should contain new Statements."
        //TODO - add new Identifier test
        testCase "removing Statements from Dataset" <| fun _ ->
            let instance = createInstance ()
            instance.CreateDataset (helloDS) |> ignore
            instance.Write helloDS (fun tx ->
                Array.iter (fun statement -> tx.AddStatement statement |> ignore) statements
                tx.RemoveStatement nemesis |> ignore
                Ok ()) |> ignore
            let result = instance.Query helloDS (fun tx -> tx.AllStatements ())
            let result = Result.map (fun statements -> Array.sort statements) result
            Expect.equal result (Ok (Array.filter (fun statement -> statement = nemesis |> not) statements)) "Dataset should contain all but removed Statements."
        testCase "matching Statements in Dataset" <| fun _ ->
            let instance = createInstance ()
            instance.CreateDataset (helloDS) |> ignore
            instance.Write helloDS (fun tx ->
                Array.iter (fun statement -> tx.AddStatement statement |> ignore) statements
                Ok ()) |> ignore
            let results = instance.Query helloDS (fun tx -> tx.MatchStatements None None None)
            let results = Result.map (fun statements -> Array.sort statements) results
            Expect.equal results (Ok statements) ""
            //TODO add more query cases
    ]

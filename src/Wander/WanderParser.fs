// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

module Ligature.Wander.Parser

open Ligature.Wander.Model
open Ligature
open Lexer

let inline todo<'T> : 'T = raise (System.NotImplementedException("todo"))

let integerNibbler =
    Nibblers.takeCond (fun (token: WanderToken) ->
        match token with
        | Integer(_) -> true
        | _ -> false)

let stringNibbler =
    Nibblers.takeCond (fun token ->
        match token with
        | StringLiteral(_) -> true
        | _ -> false)

let booleanNibbler =
    Nibblers.takeCond (fun token ->
        match token with
        | Boolean(_) -> true
        | _ -> false)

let identifierNibbler = 
    Nibblers.takeCond (fun token ->
        match token with
        | Identifier(_) -> true
        | _ -> false)

let valueNibbler =
    Gaze.map (Nibblers.takeFirst [ integerNibbler; stringNibbler; booleanNibbler; identifierNibbler ]) (fun token ->
        match token with
        | Integer(i) -> Value(WanderValue.Integer(i))
        | StringLiteral(s) -> Value(WanderValue.String(s))
        | Boolean(b) -> Value(WanderValue.Boolean(b))
        | Identifier(i) -> Value(WanderValue.Identifier(i))
        | _ -> todo) //error $"Token {e} can not start an expression or statement." None)

/// <summary></summary>
/// <param name="tokens">The list of WanderTokens to be parsered.</param>
/// <returns>The AST created from the token list of an Error.</returns>
let parse (tokens: Lexer.WanderToken list) =
    let gaze = Gaze.fromList tokens
    let res = Gaze.attempt valueNibbler gaze

    match res with
    | Some(ast) -> Ok([ ast ])
    | None -> todo

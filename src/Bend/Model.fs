// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

module Ligature.Bend.Model
open Ligature

[<RequireQualifiedAccess>]
type Expression =
    | Nothing
    | Int of int64
    | String of string
    | Bool of bool
    | Identifier of Identifier
    | Let of name: string * value: Expression
    | NamePath of string list
    | Grouping of Expression list
    | Array of Expression list
    | Application of Expression list
    | FunctionCall of name: string * arguments: Expression list
    | Record of list<string * Expression>
    | When of list<Expression * Expression>
    | Lambda of list<string> * Expression

[<RequireQualifiedAccess>]
type BendValue =
    | Int of int64
    | String of string
    | Bool of bool
    | Identifier of Identifier
    | Statement of Ligature.Statement
    | Nothing
    | Lambda of paramters: string list * body: Expression
    | HostFunction of HostFunction
    | Array of BendValue list
    | Record of Map<string, BendValue>
    //TODO add Bytes

and HostFunction(eval: BendValue list -> Bindings.Bindings<string, BendValue> -> Result<BendValue, LigatureError>) =
    member _.Run args bindings = eval args bindings

type Parameter =
    { name: string; tag: string }

type Bindings = Bindings.Bindings<string, BendValue>

let rec prettyPrint (value: BendValue): string =
    match value with
    | BendValue.Int i -> sprintf "%i" i
    | BendValue.String s -> s
    | BendValue.Bool b -> sprintf "%b" b
    | BendValue.Identifier i -> $"`{(readIdentifier i)}`"
    | BendValue.Nothing -> "Nothing"
    | BendValue.Lambda(_, _) -> "Lambda"
    | BendValue.HostFunction(_) -> "HostFunction"
    | BendValue.Array(values) -> $"[{printValues values}]"
    | BendValue.Statement(statement) -> printStatement statement
    | BendValue.Record(values) -> printRecord values

and printRecord values =
    "{ " + Map.fold 
        (fun state key value -> state + $"{key} = {prettyPrint value}, ") "" values
         + "}"

and printStatement statement =
    $"`{(readIdentifier statement.Entity)}` `{(readIdentifier statement.Attribute)}` {(printLigatureValue statement.Value)}"

and printLigatureValue value =
    match value with
    | Value.Identifier(value) -> $"`{(readIdentifier value)}`"
    | Value.Integer(value) -> value.ToString()
    | Value.String(value) -> $"\"{value}\"" //TODO escape properly

and printValues values =
    List.fold (fun x y -> x + (prettyPrint y) + ", " ) "" values

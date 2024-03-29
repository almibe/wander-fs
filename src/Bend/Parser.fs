// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

module rec Ligature.Bend.Parser

open Lexer
open FsToolkit.ErrorHandling
open Model
open Nibblers

[<RequireQualifiedAccess>]
type Element =
| NamePath of string list
| Nothing
| Grouping of Element list
| Application of Element list
| String of string
| Int of int64
| Bool of bool
| Identifier of Ligature.Identifier
| Array of Element list
| Let of string * Element
| When of (Element * Element) list
| Lambda of string list * Element
| Record of (string * Element) list

let nameStrNibbler (gaze: Gaze.Gaze<Token>) : Result<string, Gaze.GazeError> =
    Gaze.attempt
        (fun gaze ->
            match Gaze.next gaze with
            | Ok(Token.Name(value)) -> Ok(value)
            | _ -> Error Gaze.GazeError.NoMatch)
        gaze

let nameNib (gaze: Gaze.Gaze<Token>) = 
    Gaze.attempt
        (fun gaze ->
            match Gaze.next gaze with
            | Ok(Token.Name(name)) -> Ok(Element.NamePath([name]))
            | _ -> Error(Gaze.GazeError.NoMatch))
        gaze

let namePathNib = Gaze.map (repeatSep nameStrNibbler Token.Dot) (fun namePath -> Element.NamePath namePath)

let readAssignment gaze =
    Gaze.attempt
        (fun gaze ->
            result {
                let! name = Gaze.attempt nameStrNibbler gaze
                let! _ = Gaze.attempt (take Token.EqualsSign) gaze
                let! v = elementNib gaze
                return Element.Let(name, v)
            })
        gaze

let conditionsNibbler (gaze: Gaze.Gaze<Token>) =
    result {
        let! condition = Gaze.attempt elementNib gaze
        let! _ = Gaze.attempt wideArrowNib gaze
        let! body = Gaze.attempt elementNib gaze
        return (condition, body)
    }

let readWhen gaze =
    Gaze.attempt
        (fun gaze ->
            result {
                let! _ = Gaze.attempt (take Token.WhenKeyword) gaze
                let! _ = Gaze.attempt (take Token.OpenParen) gaze
                let! conditions = Gaze.attempt (repeatSep conditionsNibbler Token.Comma) gaze
                let! _ = Gaze.attempt (take Token.CloseParen) gaze
                return Element.When(conditions)
            })
        gaze

let lambdaNib gaze =
    Gaze.attempt
        (fun gaze ->
            result {
                let! _ = Gaze.attempt (take Token.Lambda) gaze
                let! parameters = Gaze.attempt (repeat nameStrNibbler) gaze
                let! _ = Gaze.attempt (take Token.Arrow) gaze
                let! body = Gaze.attempt elementNib gaze
                return Element.Lambda(parameters, body)
            }
        )
        gaze

// let readNameOrFunctionCall (gaze: Gaze.Gaze<Token>) =
//     Gaze.attempt
//         (fun gaze ->
//             match Gaze.next gaze with
//             | Ok(Token.Name(name)) ->
//                 match Gaze.peek gaze with
//                 | Ok(Token.OpenParen) ->
//                     let arguments = readArguments gaze
//                     match arguments with
//                     | Ok(arguments) -> Ok(Expression.FunctionCall(name, arguments))
//                     | _ -> Error(Gaze.GazeError.NoMatch)
//                 | _ -> Ok(Expression.Name(name))
//             | _ -> Error(Gaze.GazeError.NoMatch))
//         gaze

let readInteger (gaze: Gaze.Gaze<Token>) =
    Gaze.attempt
        (fun gaze ->
            match Gaze.next gaze with
            | Ok(Token.Int(i)) -> Ok(Element.Int(i))
            | _ -> Error(Gaze.GazeError.NoMatch))
        gaze

let applicationNib (gaze: Gaze.Gaze<Token>) = 
    Gaze.map (repeatMulti applicationInnerNib) (fun elements -> Element.Application(elements)) gaze

let equalSignNib (gaze: Gaze.Gaze<Token>) =
    Gaze.attempt
        (fun gaze ->
            match Gaze.next gaze with
            | Ok(Token.EqualsSign) -> Ok(())
            | _ -> Error(Gaze.GazeError.NoMatch))
        gaze

let wideArrowNib (gaze: Gaze.Gaze<Token>) =
    Gaze.attempt
        (fun gaze ->
            match Gaze.next gaze with
            | Ok(Token.WideArrow) -> Ok(())
            | _ -> Error(Gaze.GazeError.NoMatch))
        gaze

let arrayNib (gaze: Gaze.Gaze<Token>) : Result<Element, Gaze.GazeError> =
    result {
        let! _ = Gaze.attempt (take Token.OpenSquare) gaze
        let! values = Gaze.attempt (optional (repeatSep elementNib Token.Comma)) gaze
        let! _ = Gaze.attempt (take Token.CloseSquare) gaze
        return Element.Array(values)
    }

let groupingNib (gaze: Gaze.Gaze<Token>) : Result<Element, Gaze.GazeError> =
    result {
        let! _ = Gaze.attempt (take Token.OpenParen) gaze
        let! values = Gaze.attempt (optional (repeatSep elementNib Token.Comma)) gaze
        let! _ = Gaze.attempt (take Token.CloseParen) gaze
        return Element.Grouping(values)
    }

let declarationsNib (gaze: Gaze.Gaze<Token>) =
    result {
        let! name = Gaze.attempt nameStrNibbler gaze
        let! _ = Gaze.attempt equalSignNib gaze
        let! expression = Gaze.attempt elementNib gaze
        return (name, expression)
    }

let recordNib (gaze: Gaze.Gaze<Token>) : Result<Element, Gaze.GazeError> =
    result {
        let! _ = Gaze.attempt (take Token.OpenBrace) gaze
        let! declarations = (optional (repeatSep declarationsNib Token.Comma)) gaze
        let! _ = Gaze.attempt (take Token.CloseBrace) gaze
        return Element.Record(declarations)
    }

/// Read the next Element from the given instance of Gaze<Token>
let readValue (gaze: Gaze.Gaze<Token>) : Result<Element, Gaze.GazeError> =
    let next = Gaze.next gaze

    match next with
    | Error(err) -> Error err
    | Ok(Token.Int(value)) -> Ok(Element.Int value)
    | Ok(Token.Bool(value)) -> Ok(Element.Bool value)
    | Ok(Token.Identifier(value)) -> Ok(Element.Identifier value)
    | Ok(Token.StringLiteral(value)) -> Ok(Element.String value)
    | _ -> Error(Gaze.GazeError.NoMatch)

let applicationInnerNib = takeFirst [
    readValue; 
   // readAssignment; 
    namePathNib;
    arrayNib; 
    recordNib;
    lambdaNib;
    groupingNib;
    readWhen;
    ]

let elementNib = takeFirst [
    readAssignment; 
    applicationNib;
    namePathNib;
    readValue; 
    arrayNib; 
    recordNib;
    lambdaNib;
    groupingNib;
    readWhen;
    ]

let scriptNib = repeatSep elementNib Token.Comma

/// <summary></summary>
/// <param name="tokens">The list of Tokens to be parsered.</param>
/// <returns>The AST created from the token list of an Error.</returns>
let parse (tokens: Token list): Result<Element list, Gaze.GazeError> =
    let tokens =
        List.filter
            (fun token ->
                match token with
                | Token.Comment(_)
                | Token.WhiteSpace(_)
                | Token.NewLine(_) -> false
                | _ -> true)
            tokens
    if tokens.IsEmpty then
        Ok [Element.Nothing]
    else
        let gaze = Gaze.fromList tokens
        Gaze.attempt scriptNib gaze

/// Helper function that handles tokienization for you.
let parseString (input: string) =
    match tokenize input with
    | Ok tokens -> parse tokens
    | Error err -> Error err //error "Could not parse input." None //error $"Could not match from {gaze.offset} - {(Gaze.remaining gaze)}." None //TODO this error message needs updated
//    printfn "%A" (sprintf "%A" (tokenize input))

let expressArray values =
    let res = List.map (fun value -> express value) values
    Expression.Array res

let expressGrouping values =
    let res = List.map (fun value -> express value) values
    Expression.Grouping res

let handleRecord (declarations: list<string * Element>) =
    let res = List.map (fun (name, value) -> (name, (express value))) declarations
    Expression.Record res

let handleLambda (parameters: string list) body =
    Expression.Lambda (parameters, (express body))

let handleWhen (conditionals: list<Element * Element>) =
    let conditionals = List.map (fun (condition, body) -> ((express condition), (express body))) conditionals
    Expression.When conditionals

let expressApplication elements =
    let res = List.map (fun element -> express element) elements
    Expression.Application res

/// This will eventually handle processing pipe operators
let express (element: Element) =
    match element with
    | Element.Int value -> Expression.Int value
    | Element.Bool value -> Expression.Bool value
    | Element.NamePath namePath -> Expression.NamePath namePath
    | Element.Nothing -> Expression.Nothing
    | Element.String value -> Expression.String value
    | Element.Identifier id -> Expression.Identifier id
    | Element.Let(name,value) -> Expression.Let(name, (express value))
    | Element.Array values -> expressArray values
    | Element.Grouping elements -> expressGrouping elements
    | Element.Application elements -> expressApplication elements
    | Element.Record declarations -> handleRecord declarations
    | Element.Lambda(parameters, body) -> handleLambda parameters body
    | Element.When(conditionals) -> handleWhen conditionals

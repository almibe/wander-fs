// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

module Ligature.Bend.Interpreter

open Ligature.Bend.Model
open Ligature.Bend.Bindings
open Ligature

let readNamePath (namePath: string list) (bindings: Bindings<string, BendValue>) =
    match read (List.head namePath) bindings with
    | Some(value) -> 
        match value with
        | BendValue.Record(values) -> 
            match namePath.Tail with
            | [] -> Some (BendValue.Record values)
            | namePath -> 
                List.fold (fun values name -> 
                    match values with
                    | Some(BendValue.Record(values)) -> 
                        match Map.tryFind name values with
                        | Some(res) -> Some(res)
                        | None -> None
                    | None -> failwith "Not Implemented"
                    | Some(value) -> Some value) (Some(BendValue.Record(values))) namePath
        | value -> Some value
    | None -> None

let rec evalExpression bindings expression =
    let rec bindArguments (args: Ligature.Bend.Model.Expression list) (parameters: string list) (bindings: Bindings): Result<Bindings, LigatureError> =
        if List.length args <> List.length parameters then
            failwith "todo"
        else if List.isEmpty args && List.isEmpty parameters then
            Ok bindings
        else
            let arg = List.head args
            let parameter = List.head parameters
            let value = evalExpression bindings arg
            match value with
            | Ok (value, _) ->
                Ok(bind parameter value bindings)
            | Error(err) -> Error(err)

    match expression with
    | Expression.NamePath(name) ->
        match readNamePath name bindings with
        | Some(value) -> Ok((value, bindings))
        | None -> error $"Could not read {name}" None
    | Expression.Grouping(expressions) ->
        let bindings' = addScope bindings
        match evalExpressions bindings' expressions with
        | Error(err) -> Error(err)
        | Ok((res, _)) -> Ok((res, bindings))
    | Expression.Let(name, expression) ->
        let res = evalExpression bindings expression

        match res with
        | Ok((value, _)) ->
            let bindings = bind name value bindings
            Ok((value, bindings))
        | Error(_) -> res
    | Expression.FunctionCall(name, args) -> failwith "TODO"
        // let args = List.map ( fun a -> 
        //     match evalExpression bindings a with
        //     | Ok(v, _) -> failwith "todo" //Expression.Value(v)
        //     | Error(err) -> todo)
        //             args
    //     match Bindings.read name bindings with
    //     | Some(BendValue.HostFunction(funct)) -> 
    //         match funct.Run args bindings with
    //         | Ok res -> Ok (res, bindings)
    //         | Error(err) -> Error(err)
    //     // | Some(BendValue.Lambda(parameters, body)) ->
    //     //     match bindArguments args parameters bindings with
    //     //     | Ok(bindings) -> evalExpression bindings body
    //     //     | Error(err) -> Error(err)
    //     | None -> error $"{name} function not found." None
    //     | _ -> todo //type error
    // // | Expression.Conditional(conditional) ->
    // //     let ifCondition = evalExpression bindings conditional.ifCase.condition
    // //     let mutable result = None

    //     result <-
    //         match ifCondition with
    //         | Ok(BendValue.Bool(true), bindings) -> Some (evalExpression bindings conditional.ifCase.body)
    //         | Ok(BendValue.Bool(false), _) -> None
    //         | Ok _ -> Some(error "Type mismatch, expecting boolean." None)
    //         | Error x -> Some(Error x)

        // let mutable elsifCases = conditional.elsifCases
        // while Option.isNone result && not elsifCases.IsEmpty do
        //     let case = elsifCases.Head
        //     result <- 
        //         match evalExpression bindings case.condition with
        //         | Ok(BendValue.Bool(true), bindings) -> Some (evalExpression bindings case.body)
        //         | Ok(BendValue.Bool(false), _) -> None
        //         | Ok _ -> Some(error "Type mismatch, expecting boolean." None)
        //         | Error x -> Some(Error x)
        //     elsifCases <- elsifCases.Tail

        // match result with
        // | None -> evalExpression bindings conditional.elseBody
        // | Some(result) -> result
    | Expression.Nothing -> Ok (BendValue.Nothing, bindings)
    | Expression.Int value -> Ok (BendValue.Int value, bindings)
    | Expression.String value -> Ok (BendValue.String value, bindings)
    | Expression.Bool value -> Ok (BendValue.Bool value, bindings)
    | Expression.Identifier id -> Ok (BendValue.Identifier id, bindings)
    | Expression.Array(expressions) ->
        let mutable error = None
        let res: BendValue list = 
            //TODO this doesn't short circuit on first error
            List.map (fun e ->
                match evalExpression bindings e with
                | Ok(value, _) -> value
                | Error(err) -> 
                    if Option.isNone error then error <- Some(err)
                    BendValue.Nothing
                        ) expressions
        match error with
        | None -> Ok((BendValue.Array(res), bindings))
        | Some(err) -> Error(err)
    | Expression.Lambda(parameters, body) -> handleLambda bindings parameters body
    | Expression.Record(values) -> handleRecord bindings values
    | Expression.When(conditionals) -> handleWhen bindings conditionals
    | Expression.Application(values) -> handleApplication bindings values

and handleRecord bindings values =
    let res = List.map (fun (name, expr) -> 
        match evalExpression bindings expr with
        | Error(err) -> failwith "TODO"
        | Ok((res, _))    -> (name, res)) values
    let v = List.fold (fun state (name, value) -> Map.add name value state) (Map []) res
    Ok (BendValue.Record (v), bindings)

and handleApplication bindings values =
    let arguments = List.tail values
    match List.tryHead values with
    | Some(Expression.NamePath(functionName)) ->
        match readNamePath functionName bindings with
        | Some(BendValue.Lambda(parameters, body)) -> evalLambda bindings parameters body arguments
        | Some(BendValue.HostFunction(hostFunction)) -> evalHostFunction bindings hostFunction arguments
        | Some(BendValue.Array(values)) -> evalArray bindings values arguments
        | Some(BendValue.Identifier(entity)) -> evalStatement bindings entity arguments
        | Some(_) -> failwith "Not Implemented"
        | None -> failwith $"Function {functionName} not found."
    | Some(Expression.Identifier(entity)) -> evalStatement bindings entity arguments
    | Some(_) -> error "Invalid Application." None
    | None -> error "Should never reach, evaling empty Application." None

and evalStatement bindings (entity: Identifier) arguments =
    match arguments with
    | [Expression.Identifier(attribute); value: Expression] ->
        let value = 
            match value with
            | Expression.Identifier(value) -> Value.Identifier(value)
            | Expression.Int(value) -> Value.Integer(value)
            | Expression.String(value) -> Value.String(value)
            | _ -> failwith ""
        Ok((BendValue.Statement({ Entity = entity; Attribute = attribute; Value = value } ), bindings))
    | _ -> failwith ""

and evalHostFunction bindings hostFunction arguments =
    let values = List.map (fun arg -> 
        match evalExpression bindings arg with
        | Ok((value, _)) -> value
        | Error(err) -> failwith "TODO") arguments
    match hostFunction.Run values bindings with
    | Ok(res) -> Ok(res, bindings)
    | Error(err) -> Error(err)

and evalArray bindings array arguments =
    match arguments with
    | [Expression.Int(index)] -> Ok(array.Item(int32(index)), bindings)
    | _ -> failwith ""

and evalLambda bindings parameters body arguments =
    let mutable i = 0
    let mutable error = None
    let args = Array.init (List.length parameters) (fun i -> BendValue.Nothing)
    List.tryFind (fun arg -> 
        match evalExpression bindings arg with
        | Ok((res, _)) ->
            Array.set args i res
            i <- i + 1
            false
        | Error(err) ->
            error <- Some(err)
            true) arguments |> ignore
    match error with
    | Some(err) -> Error(err)
    | None ->
        let mutable scope = Bindings.addScope bindings
        List.iteri (fun i arg -> scope <- Bindings.bind (List.item i parameters) arg bindings) (Array.toList args)
        evalExpression scope body

and handleLambda bindings parameters body =
    Ok(BendValue.Lambda(parameters, body), bindings)

and handleWhen bindings conditionals =
    match List.tryFind (fun (condition, body) -> 
        match evalExpression bindings condition with
        | Ok((BendValue.Bool(value), _)) -> value
        | _ -> false
        ) conditionals with
    | Some((_, body)) -> evalExpression bindings body
    | None -> error "No branches matched in when expression." None

and evalExpressions
    (bindings: Bindings.Bindings<_, _>)
    (expressions: Expression list)
    : Result<(BendValue * Bindings.Bindings<_, _>), LigatureError> =
    match List.length expressions with
    | 0 -> Ok(BendValue.Nothing, bindings)
    | 1 -> evalExpression bindings (List.head expressions)
    | _ ->
        let mutable result = Ok(BendValue.Nothing, bindings)
        let mutable cont = true
        let mutable bindings = bindings
        let mutable expressions = expressions
        while cont && not (List.isEmpty expressions) do
            result <- evalExpression bindings (List.head expressions)
            expressions <- List.tail expressions
            match result with
            | Ok((res, b)) ->
                bindings <- b
                result <- Ok((res, b))
            | Error(err) ->
                result <- Error(err)
                cont <- false
        result

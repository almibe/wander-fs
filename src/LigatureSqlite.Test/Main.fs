﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

module Ligature.Sqlite.Test

open Ligature.Sqlite.Main
open Expecto
open Ligature.TestSuite

[<Tests>]
let tests = ligatureTestSuite (fun () -> ligatureSqlite InMemory)

[<Tests>]
let bendTests = bendTestSuite (fun () -> ligatureSqlite InMemory)

[<EntryPoint>]
let main argv =
    runTestsInAssemblyWithCLIArgs [] argv

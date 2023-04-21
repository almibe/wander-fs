# ligature-fs
An experimental F# based implementation of Ligature.
This is still very early in development and not ready for any sort of use yet.
Keep in mind that the docs below might not be up to date.
See https://ligature.dev for more information on Ligature.

## Use
This project assumes you have .NET Core setup.

See https://dotnet.microsoft.com/en-us/download to download the SDK for .NET Core.

After installing .NET Core the following command should run the test suite for Ligature.

```
dotnet run --project src/Ligature.Test
```

## Projects

This repo is made up of a couple of different projects, below is a description of each.
Some of these projects have a sibling project like Ligature.Test that simply contains a
test suite for the other project.

| Project           | Description                                                        |
| ----------------- | ------------------------------------------------------------------ |
| build             | Fake build script.                                                 |
| Gaze              | A parsing library.                                                 |
| Lig               | Support for the Lig serialization language.                        |
| Ligature          | Main code base including the main logic and types.                 |
| LigatureFable     | A project for producing JavaScript code using Fable 4.             |
| LigatureHttp      | A web backend for working with Ligature.                           |
| LigatureInMemory  | An implementation of Ligature that uses in-memory data structures. |
| LigatureLMDB      | An implementation of Ligature that uses LMDB for storage.          |
| LigatureSqlite    | An implementation of Ligature that uses SQLite3 for storage.       |
| LigatureTestSuite | A common test suite for Ligature implementations*.                 |
| Wander            | The Wander scripting language.                                     |
| Wander.X          | An experimental rewrite of the Wander scripting language.          |

*Note: LigatureTestSuite is intended to be used by implementations in their own test suite. It can't be ran by itself.*

## Setup

After checking out this project run.

`dotnet tool restore`

This is will setup tools used by this project.

## Formatting

Examples running the formatter

`dotnet fantomas ./src/Ligature/`

`dotnet fantomas ./ -r`

## Linting

Examples running the linter

`dotnet fsharplint lint ./src/Lig/LigRead.fs`

## LigatureHttp

LigatureHttp is a project that allows running Wander code against an instance of Ligature via HTTP.

### Running LigatureHttp

`dotnet run --project src/LigatureHttp`

### Building LigatureHttp

To build for your current platform

```
cd src/LigatureHttp
dotnet publish
```

See https://learn.microsoft.com/en-us/dotnet/core/deploying/ for more information.

## LigatureFable

LigatureFable is a project that uses [Fable](https://fable.io) to generate an NPM package for Ligature projects that are compatiable with the browser and node.

### Building LigatureFable

Run the following commands from the root of this project (and yes there are two src dirs that's not a typo).

```
cd src/LigatureFable/src
dotnet fable -o npmProject
```

This is generate all of the JavaScript files and place them in the npmProject subdirectory.
If you then go into that directory you'll notice that there is a package.json file there already.
All .js files are in the .gitignore file for this subproject since they are generated.

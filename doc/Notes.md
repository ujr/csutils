
Miscellaneous Notes
===================


Tooling
-------

[Visual Studio Code][vscode] is a freely available source code editor
by Microsoft. It is independent from the commercial Visual Studio IDE.
Developed mainly in Zurich under the lead of Erich Gamma.

[C# for Visual Studio Code][csext] is an extension for VS Code that
provides editing and debugging support, development tools, and support
for *.csproj* files.

[xUnit.net][xunit] is a free and open-source unit testing framework
for .NET under the .NET Foundation.

[EditorConfig][edconf] helps maintain consistent coding style across editors
by means of a file *.editorconfig* that documents your style settings.
Many editors honour *.editorconfig* directly or via an extension.

Git ignore (.gitignore) template created with
[www.gitignore.io](http://www.gitignore.io/) (GitHub also has a
[repo with .gitignore templates](https://github.com/github/gitignore)).


Concepts & Guides
-----------------

**.NET Core** is an open source and cross-platform (Windows,
Linux, macOS) implementation of the [.NET Standard][netstandard].
It is documented in Microsoft's [.NET Core Guide][netcore],
which includes sources for download of the SDK and the runtime.
The C# language is documented in the [C# Guide][csharp].

The .NET Core Guide has a useful section about
[**Unit Testing** Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices).

 - Test naming: *method_scenario_behaviour* (may yield very many tests)
 - Test structure: **arrange, act, assert** (known as the "3A pattern")
 - Mocks and Stubs are Fakes;
   a **Stub** simply stands in for a dependency
   whereas a **Mock** is asserted against.

**JSON** (JavaScript Object Notation) is a plain-text exchange format
based on the object literal notation of JavaScript. The MIME Type
is `application/json`, the file extension typically `.json`, and
the website is at [json.org][json].

What **open-source license** for your project?
Try [choosealicense.com](https://choosealicense.com/)
created by the GitHub folks.
Other options are [opensource.org](https://opensource.org/)
and [unlicense.org](https://unlicense.org/).


The .NET Core CLI
-----------------

.NET Core comes with a CLI (command line interface) called `dotnet`.
It can be used to restore dependencies, build the project, run unit
tests, etc. It is documented in the [.NET Core Guide][netcore].

To show the version or detailed information
about this .NET Core SDK installation:

> `dotnet --version`  
> `dotnet --info`  

Projects are created with `dotnet new TYPE` where TYPE is
one of `console`, `classlib`, `xunit` (xUnit test project),
`webapp` (ASP.NET Core web app), and many others:

> `dotnet new console`  
> `dotnet new classlib`  
> `dotnet new xunit`  
> `dotnet new --help`

To reference other projects and packages, add `ProjectRefernence`
and `PackageReference` elements to the project file (.csproj) and
run `dotnet restore`. Alternatively, and more conveniently:

> `dotnet add reference PATH/FOO.csproj`  
> `dotnet add package PACKAGE_NAME`  

A solution file (.sln) holds together several projects (.csproj).
Use `dotnet new sln` to create a solution file, and `dotnet sln`
to add, remove, and list projects in a solution.

> `dotnet new sln`  
> `dotnet sln add PATH/FOO.csproj`

Once everything is set up, a project (or all projects in a solution)
is managed with the following commands:

> `dotnet restore` // restore project dependencies (using NuGet)  
> `dotnet build`   // build the project (and all its dependencies)  
> `dotnet test`    // find and run unit tests in the project  
> `dotnet clean`   // clean the output of a project  

For further capabilities of the CLI, refer to the .NET Core Guide.


[vscode]: https://code.visualstudio.com/
[xunit]: https://xunit.net/
[edconf]: https://editorconfig.org/

[dotnet]: https://docs.microsoft.com/en-us/dotnet/
[netcore]: https://docs.microsoft.com/en-us/dotnet/core/
[netstandard]: https://docs.microsoft.com/en-us/dotnet/standard/
[csharp]: https://docs.microsoft.com/en-us/dotnet/csharp/
[json]: https://json.org/

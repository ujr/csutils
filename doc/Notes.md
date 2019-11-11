
Miscellaneous Notes
===================


Tooling
-------

[Visual Studio Code][vscode] is a freely available source code editor 
by Microsoft. It is independent from the commercial Visual Studio IDE. 
Developed mainly in Zurich under the lead of Erich Gamma.

[xUnit.net][xunit] is a free and open-source unit testing framework 
for .NET under the .NET Foundation. 

[EditorConfig][edconf] helps maintain consistent coding style across editors 
by means of a file *.editorconfig* that documents your style settings. 
Many editors honour *.editorconfig* directly or via an extension.

Git ignore (.gitignore) template created with 
[www.gitignore.io](http://www.gitignore.io/) (GitHub also has 
a [repo with .gitignore templates](https://github.com/github/gitignore)).


Concepts & Guides
-----------------

**.NET Core** is documented in Microsoft's [.NET Core Guide][core], 
which includes sources for download of the SDK and the runtime.

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

What **open-source license?** 
Try [choosealicense.com](https://choosealicense.com/)
created by the GitHub folks.
Other options are [opensource.org](https://opensource.org/) 
and [unlicense.org](https://unlicense.org/).


[vscode]: https://code.visualstudio.com/
[xunit]: https://xunit.net/
[edconf]: https://editorconfig.org/

[core]: https://docs.microsoft.com/en-us/dotnet/core/
[json]: https://json.org/

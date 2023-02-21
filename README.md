This repository holds the standard libraries for [vvvv](https://visualprogramming.net), a visual live-programming environment for .NET. It is targeted at developers who want to fix/improve/add-to specifically these libraries. 

The individual libraries are organized in directories. Each directory starting with "VL." holds the sources of one library. Each library is built to a [NuGet](https://www.nuget.org/) and NuGets can be referenced as a dependency by .vl documents. 

## Working with this repository
Working with this repository requires two steps:
- Build `VL.StandardLibs.sln` using Visual Studio 2022
- Run vvvv with this directory as a [source package-repository](https://thegraybook.vvvv.org/reference/extending/contributing.html)

## Contributing to this repository
- Consider starting a discussion around your proposed changed in an issue, before starting your work
- Keep the pull-request as minimal as possible
- Sign the CLA

## License
Many VL sources in this repository are mere wrappers around original .NET libraries that come with their own open-source licenses. If not specified otherwise, sources in this repository are licensed under the [LGPLv3](https://www.gnu.org/licenses/lgpl-3.0-standalone.html).

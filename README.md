# Standard Libraries for vvvv

![vvvv](docs/vvvvIO.png)

## Working with this repository

If you're merely using vvvv, this repository is not for you. It is only useful for developers who want to fix/improve/add-to libraries that are part of this repository.

The individual libraries are organized in directories. Each directory starting with "VL." holds the sources of one library. 

Working with this repository requires two steps:
- Build `VL.StandardLibs.sln` using Visual Studio 2022
- Run vvvv with this directory as a [source package-repository](https://thegraybook.vvvv.org/reference/extending/contributing.html)

## Contributing to this repository

Before getting to work, please start a discussion around your proposed changes in an issue, to:
- Make sure that no one else is working on that same topic already
- Lay out your plans and discuss them with others to make sure your idea is properly architectured and would fit well with the project

## License
Many VL sources in this repository are mere wrappers around original .NET libraries that come with their own open-source licenses. If not specified otherwise, sources in this repository are licensed under the [LGPLv3](https://www.gnu.org/licenses/lgpl-3.0-standalone.html).

# Standard Libraries for vvvv

![vvvv](.github/vvvvIO.png)

To learn more about vvvv, visit: [visualprogramming.net](https://visualprogramming.net).  
For dev-talk around libraries in this repository join our [VL.StandardLibs chat](https://matrix.to/#/#VL.StandardLibs:matrix.org).

## Working with this repository

If you're merely using vvvv, this repository is not for you. It is only useful for developers who want to fix/improve/add-to libraries that are part of this repository.

The individual libraries are organized in directories. Each directory starting with "VL." holds the sources of one library. 

Here are the steps required to work with this repository
- Build `VL.StandardLibs.sln` using Visual Studio 2022 (>= Version 17.5.1)
- Run vvvv with this directory as a [source package-repository](https://thegraybook.vvvv.org/reference/extending/contributing.html)

At this point you've replaced all libraries shipping with your vvvv installation with the ones in the repository. This means you're now running them "from source" and could e.g. switch to other branches. Still at this point you'll not be able to edit files, because by default they are precompiled! To enable editing of files for specific libraries you now have to run vvvv with another commandline argument that specifies which of the libraries you want to work on, like so:

- `--editable-packages VL.Stride.*;VL.Skia`

Like this you still get the fast startup-time for all the other libraries that you don't work on.

For agents migrating legacy VL nodes to C# imports, see the [C# import migration guide](AGENTS.md).

## Contributing to this repository

Please see our [Contribution Guide](.github/CONTRIBUTING.md).

## Line endings

Line endings are handled by git through `.gitattributes`, for everyone and independent of personal settings like `core.autocrlf`: text files are stored with LF, checked out with the platform's native line ending (CRLF on Windows, LF on Linux/macOS), and normalized again on commit. You (and coding agents) don't need to care about line endings in editors; diffs never show line ending changes.

Run once per clone, so that `git blame` skips the commit that normalized all files (GitHub does this automatically):

```
git config blame.ignoreRevsFile .git-blame-ignore-revs
```

Before merging or rebasing a branch that was started before the normalization (November 2026), enable renormalizing merges, otherwise every file the branch touched conflicts over line endings:

```
git config --global merge.renormalize true
```

(or per merge: `git merge -X renormalize <branch>`)

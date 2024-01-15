# VL.ImGui

A node set around [ImGui](https://github.com/ocornut/imgui). Use the [VL.ImGui.Skia](../VL.ImGui.Skia) package to render the UI in Skia.

Most of the nodes get generated with a [C# source generator](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview).
It can be configured with the `GenerateNode` attribute.
Help texts can be added to nodes and pins via `summary` XML comments.

## Updating ImGui
We're using a custom build of `cimgui` where the ImGui context is thread static (https://github.com/vvvv/VL.ImGui/issues/2). In order to update the following steps have to be done:
- Update the ImGui.NET pacakge
- Update the submodule ImGui.NET-nativebuild to the same version as the package by merging the remote changes. There will be conflicts in the generator output files, choose remote
- Regenerate the bindings with generator.bat - to run the generator you need to extract luajit (from https://luapower.com/luajit) and mingw64 (from https://github.com/niXman/mingw-builds-binaries/releases) and add both their bin folders to the path variable inside the generator.bat file.
- Compile the lib with `build-native.cmd Release`
- Copy the generated `cimgui.dll` to `runtimes\win-x64\native`

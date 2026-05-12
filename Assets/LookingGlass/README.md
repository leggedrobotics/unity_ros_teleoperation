# The LookingGlass Unity Plugin
v3.2.8

## Docs & Examples
Please visit the [Looking Glass docs site](https://docs.lookingglassfactory.com/developer-tools/unity) for explanations of how everything works.
Also, see the example scenes in Assets/LookingGlass/Examples; they are a good way to learn about the features of the plugin.


## Note About Asmdefs
**NOTE:** Our code uses Unity AssemblyDefinitions (.asmdefs).

If you wish to write code that interfaces with our code, please note that in order to reference code that already **is** in an asmdef, your code must also be within an asmdef.
Thus, perform the following:

1. Navigate in a Project View to your own C# scripts in Unity.
2. If you don't have one already, Right-click -> Create -> Assembly Definition.
3. Select your project's AssemblyDefinition asset(s), and add a reference the LookingGlass and/or LookingGlass.Editor assemblies
4. Click "Apply" in the inspector.

Most of our code can be used with the following C# using statements:
```cs
using LookingGlass;
using LookingGlass.Editor;
```


## Conditional Compilation symbols
- HAS_NEWTONSOFT_JSON
- DMA_DEV
- LKG_ASPECT_ADJUSTMENT
    - Adds aspect adjustment support in MultiViewRendering's quilt helper methods. This allows you to stretch or squash the quilt tiles (single-views) during rendering operations, when needed. This comes at a performance cost, however, so keep this undefined for maximum performance.

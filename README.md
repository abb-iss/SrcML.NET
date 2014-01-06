# ABB Build Scripts

The ABB build scripts are used by numerous projects ([SrcML.NET](https://github.com/abb-iss/SrcML.NET), [Sando](http://sando.codeplex.com/), [Swum.NET](https://github.com/abb-iss/Swum.NET)) for automatically building and versioning our software.

## Adding the scripts to your repository

In order to add the scripts to your repository, you should add them as a *[git subtree](http://blogs.atlassian.com/2013/05/alternatives-to-git-submodule-git-subtree/)*. In short:

1. Add a remote to your repository for the build scripts

This would look like this:

    git remote add -f BuildScripts https://github.com/abb-iss/BuildScripts.git
    git subtree add --prefix External/BuildScripts BuildScripts/master --squash

If you later need to update the build scripts, you can do it like this:

    git fetch BuildScripts
    git subtree pull --prefix External/BuildScripts BuildScripts master --squash

## Using the scripts

The key part of this script is the `Version.targets` file. It requires some setup in order to work:

1. `$(MSBuildCommunityTasksPath)`: this is the directory that contains the [MS Build Community Tasks](https://github.com/loresoft/msbuildtasks). It can either be installed locally in the repository or system wide.
2. Define an `AssemblyInfoFiles` item group that points to all of the assembly info files to be overwritten. The best way to do this is to have a solutin-wide "SolutionInfo.cs" that has all of the attributes. The individual projects can then have more specific information.
3. Define a `SourceManifests` item group that points to all of the `source.extension.vsixmanifest` files to be updated.
4. Add `CreateAssemblyInfo` and `SetVsixVersion` as dependencies via [`DependsOnTargets`](http://msdn.microsoft.com/en-us/library/t50z2hka.aspx)

You can see a sample of how it is used in `Build.proj`. `Build.proj` can be used to build simple projects. To perform additional actions (such as running tests or generating documentation) you should write your own build script.
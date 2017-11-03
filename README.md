# Distributing Applications with NuGet

This project is an example of how to use NuGet to distribute desktop applications, if you want to know more details access the article in Portuguese at the link below.



# Flowchart of operation
![alt text](https://raw.githubusercontent.com/xwellingtonx/NugetAutoUpdateExample/master/images/flowChart.png "Flowchart of operation")
 

# How to use?

1 - Build the ConsoleApp project in Release mode and create the NuGet Package with the command:
```
nuget pack Package.nuspec -properties Configuration=Release
```
2 - Copy the created package to the Packages directory of the MyNuGetServer project and then build the project
3 - Finally build the ConsoleApp.Launcher project

You will get a result similar to this case the package is large enough not to be downloaded and installed quickly

# Flowchart of operation
![alt text](https://raw.githubusercontent.com/xwellingtonx/NugetAutoUpdateExample/master/images/launcher.gif "ConsoleApp.Launcher")

# References
Creating NuGet packages: https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package

.Nuspec Reference: https://docs.microsoft.com/en-us/nuget/schema/nuspec

Play with Packages, programmatically!: https://blog.nuget.org/20130520/Play-with-packages.html

NuGet.Server: https://docs.microsoft.com/en-us/nuget/hosting-packages/nuget-server

Hosting your own NuGet feeds: https://docs.microsoft.com/en-us/nuget/hosting-packages/overview

NuGet2: https://github.com/NuGet/NuGet2


Copyright 2017 Wellington Pires

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

This project includes tools that facilitate the building, deployment and execution of Sitecore installation packages. 

# NuGet package

These files needed to use this project can be added to your Visual Studio project using the NuGet package [`Sitecore.Strategy.Packages`](https://www.nuget.org/packages/Sitecore.Strategy.Package).

> This package has a dependency on `Sitecore.Core`. This package is not available on public NuGet repositories. It is available when you install the [NuGet Server for Sitecore](https://github.com/adamconn/sitecore-nuget-server) module.

# Components

This project includes the following components:

* [Post steps](#poststeps)

## <a name="poststeps">Post steps</a>

A post step is a class that encapsulates logic that the Installation Wizard executes after the files and Sitecore items in an installation package are installed.

An installation package may only use one post step. The post step for an installation package is configured in the *Metadata* section of the Package Designer. This is the same place where the package name, author and other descriptive values are specified. 

In order to use these post steps the following dlls must be included in your Sitecore installation package:

* `Microsoft.Web.XmlTransform.dll`
* `Sitecore.Strategy.Packages.dll`

## XDT Transformer

This post step executes [XDT transformations](https://msdn.microsoft.com/en-us/library/dd465326%28v=vs.110%29.aspx) using files on the Sitecore server. 

The transformer displays a warning before it makes any changes and backs-up all files that it changes. The transformer also logs all of its activity at the `INFO` level.

#### Type

This post step is implemented in the type 
`Sitecore.Strategy.Packages.PostSteps.XdtTransformer, Sitecore.Strategy.Packages`. 

#### Custom attributes
This type requires the following custom attributes be specified in the Package Designer: 

* `source` - Path for the file containing the transformation instructions. This attribute usually will identify a file that is included in the installation package. 
* `target` - Path for the file that should be transformed. This attribute usually will identify a file that exists on the Sitecore server prior to the Installation Wizard running, such as `web.config`. 

> Path values are relative to the Sitecore web root folder. (To be more precise, paths are relative to the folder identified by `System.AppDomain.CurrentDomain.BaseDirectory`.) If the file you want to specify is located in the web root folder you can simply use the file name. 

#### Notes about security

This transformer modifies files. It is subject to standard Windows permissions. The ASP.NET application pool for the Sitecore website runs using a specific account. That account must have write-access to the files being changed.

If you include this functionality in your Sitecore installation package it is a good idea to include this information so users can be sure to check the file permissions before running the installation package. 

#### Example
The following example demonstrates how to use the transformer to enable [ASP.NET compatibility mode](https://msdn.microsoft.com/en-us/library/system.servicemodel.configuration.servicehostingenvironmentsection.aspnetcompatibilityenabled(v=vs.110).aspx).

##### Source file
Create a file named `web.config.transform.aspnetcompatibility` and add the following code to the file:

	<configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
	  <system.serviceModel xdt:Transform="InsertIfMissing">
	    <serviceHostingEnvironment xdt:Transform="InsertIfMissing" />
	    <serviceHostingEnvironment xdt:Transform="SetAttributes" aspNetCompatibilityEnabled="true" />
	  </system.serviceModel>
	</configuration>

##### Sitecore installation package configuration

1. Create a new installation package using the Sitecore Package Designer.
2. Add the file `web.config.transform.aspnetcompatibility` to the package so the file is included in the web root folder.
3. Add the dll `Microsoft.Web.XmlTransform.dll` to the package.
4. Add the dll `Sitecore.Strategy.Packages.dll` to the package.
5. In the *Metadata* section of the package configuration set the *Post Step* value to `Sitecore.Strategy.Packages.PostSteps.XdtTransformer, Sitecore.Strategy.Packages`
6. In the *Custom Attributes* section add an attribute named `source` and set the value to `web.config.transform.aspnetcompatibility`
7. In the *Custom Attributes* section add an attribute named `target` and set the value to `web.config`
8. Save the package and generate the ZIP.

When the package is installed using the Sitecore Installation Wizard the user will be prompted to modify the file `web.config`. If the user clicks OK:

* A backup copy of `web.config` will be created
* the XDT transformation specified in the file `web.config.transform.aspnetcompatibility` will be applied to `web.config`

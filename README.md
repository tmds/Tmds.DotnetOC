# Tmds.DotnetOC

Tmds.DotnetOC is an _experimental, prototype_ tool to interact with OpenShift during .NET Core development.

The tool is works in combination with other cli tools, namely `oc`, `git` and `dotnet`.
The tool will use context from those tools such as the current OpenShift project and git repository.

## Installing/Updating

```
dotnet tool uninstall -g Tmds.DotnetOC
dotnet tool install -g Tmds.DotnetOC --add-source https://www.myget.org/F/tmds/api/v3/index.json --version '0.1.0-*'
```

## Example

Login to OpenShift and create a project:

```
$ oc login ...
$ oc new-project example
```

Clone .NET Core application:

```
$ git clone https://github.com/redhat-developer/s2i-dotnetcore-ex
```

Deploy it to OpenShift:

```
$ dotnet oc app new s2i-dotnetcore-ex/app
```

List the applications:
```
$ dotnet oc app list
```

Query status of the deployed application named 'app':
```
$ dotnet oc app status app
```

## Commands

`dotnet oc install`

_Description:_

Installs/Updates .NET Core versions.

Example:
```
$ dotnet oc install
```

`dotnet oc app new <myproj.cs>`

_Description:_

Deploys a .NET Core application to OpenShift.

Example:
```
$ git clone https://github.com/redhat-developer/s2i-dotnetcore-ex
$ dotnet oc app new s2i-dotnetcore-ex/app
```

`dotnet oc app list`

_Description:_

Lists .NET Core applications deployed on OpenShift.

Example:
```
$ dotnet oc app list
```

`dotnet oc app status <name>`

_Description:_

Provides overview of deployed application.

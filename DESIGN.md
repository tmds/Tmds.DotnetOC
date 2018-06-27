# Design considerations

## Developer context aware

The tool is works in combination with other cli tools, namely `oc`, `git` and `dotnet`.
The tool will use context from those tools such as the current OpenShift project and git repository.

The tool can be used outside of a git repository. In that case, the user needs to be specify some parameters that would otherwise be derived from the context.

## Deployment as primary entity

OpenShift has one-to-many relationships among its resources.

```
---------        -----------        --------------------        ---------------
| Route | 1 -- N | Service | 1 -- N | DeploymentConfig | 1 -- N | BuildConfig |
---------        -----------        --------------------        ---------------
```

The tool uses the `DeploymentConfig` as the primary entity. We assume a deployment has at most 1 matching s2i dotnet buildconfig.

The `name` parameter used by the tool identifies the DeploymentConfig.

When the tool is used in a git repository, the git remote is used to find matching (s2i) BuildConfigs. When there are multiple BuildConfigs, there are also multiple DeploymentConfigs (for example, different branches of the same repository are deployed). In this case, the tool cannot unambiguously identify the deployment and the user must provide the `name` parameter.

## Matching remote urls

To match our local git repository with s2i buildconfigs, we use the remote url named `openshift-origin` when that exists and `origin` otherwise.

## Not an `oc` replacement

The tool is meant to facilicate .NET development. It is not the goal to provide an alternative command for each `oc` command.

# Commands

`dotnet-oc install`

_Description:_

Installs/Updates .NET Core support in the current OpenShift context.

_Options:_

| name | description | default |
|------|-------------|---------|
| system | Installs images streams in the `openshift` namespace | |

`dotnet-oc app new`

_Description:_

Deploys a .NET Core application to OpenShift.
This results in the creation of a BuildConfig, DeploymentConfig and Service.
The command shows the log of the build and deployment.
It returns 0 when the application was succesfully deployed and 1 otherwise.

_Parameters:_

| name | description | default |
|------|-------------|---------|
| name | name of service, buildconfig, deploymentconfig | based on startup-project name |
| git-url | url of the git repo | origin of git repo |
| git-ref | branch, tag or ref | current branch in git repo |
| sdk-version | default sdk version | current version of the sdk |
| memory-limit | memory assigned to container (MB) | 100 |
| startup-project | startup project | ??proj file in current dir |
| runtime-version | .NET Core runtime version | derived from csproj TargetFramework |

_Options:_

| name | description | default |
|------|-------------|---------|
| y | Assume answer to question is yes | _(prompt)_ |
| delete-on-fail | Deletes resources when deployment failed | |

_Derived parameters:_

| name | description | default |
|------|-------------|---------|
| image-namespace | namespace of dotnet image streams | location of dotnet runtime-version image stream |

_Example:_

```
~/repos/myrepo/src/Org.MyService (master)$ dotnet-oc app new
Creating application:
- name:              myservice
- git-url:           http://github.com/org/myrepo.git
- git-ref:           master
- startup-project:   src/Org.MyService
- runtime-version:   2.1
- sdk-version:       2.1.300
- memory-limit (MB): 100
- context:           mynamespace/192-168-42-216:8443/developer

Is this ok [y/N]:
```

_Notes:_

A new application is always created with `1` replica. This makes it simpler to represent the creation/failure on the console.

`dotnet-oc app update`

_Description:_

Changes settings on a deployed .NET Core application.
If the changes require a rebuild of the application, the build log is shown on the console.
Then progress is shown of the deployment on pods.
The command returns 0 when all pods deployed the update succesfully and 1 otherwise.

_Parameters:_

| name | description | default |
|------|-------------|---------|
| name | identifies the deployment | derived from git context |
| git-url | url of the git repo | |
| git-ref | branch, tag or ref | |
| sdk-version | default sdk version | |
| memory-limit | memory assigned to container (MB) | |
| replicas | number of replicas | |
| startup-project | startup project | |
| runtime-version | .NET Core runtime version | |

`dotnet-oc app list`

_Description_

Lists .NET Core applications deployed on the current OpenShift context.

_Example_:

```
~$ dotnet-oc app list
DEPLOYMENT  REPLICAS  SERVICES   ROUTES  REPOSITORY#REF                           PROJECT
myservice   1/1       myservice          http://github.com/org/myrepo.git#master  src/Org.MyService
```

`dotnet-oc app status`

_Description_

Provides an overview of the deployed application.

_Parameters:_

| name | description | default |
|------|-------------|---------|
| name | identifies the deployment | derived from git context |

_Example_:

```
~$ dotnet-oc app status
Build pod:
NAME                    STATUS     REF      STARTED      DURATION
dotnet-example-1-build  Completed  1ded43a  6 hours ago  2m56s

Application pods:
NAME                    READY      STATUS   RESTARTS     AGE
dotnet-example-1-hqjz2  1/1        Running  0            6h
dotnet-example-1-7cqc6  1/1        Running  0            6h
```

`dotnet-oc app delete`

_Description_

Deletes all resources associated with a deployment application.

_Parameters:_

| name | description | default |
|------|-------------|---------|
| name | identifies the deployment | derived from git context |
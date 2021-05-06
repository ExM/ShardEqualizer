[![NuGet Version](http://img.shields.io/nuget/v/ShardEqualizer.svg?style=flat)](https://www.nuget.org/packages/ShardEqualizer/) 
[![NuGet Downloads](http://img.shields.io/nuget/dt/ShardEqualizer.svg?style=flat)](https://www.nuget.org/packages/ShardEqualizer/)

# ShardEqualizer

Console utility for aligning the size of stored data between shards of the MongoDB cluster.

## Install

.NET Core tool - [ShardEqualizer](https://www.nuget.org/packages/ShardEqualizer/) nuget package.
You can install it as a global or local dotnet tool:
  ```shell
  dotnet tool install -g ShardEqualizer --version 1.0.0-beta2
  ```
or local dotnet tool:
  ```shell
  dotnet new tool-manifest
  dotnet tool install ShardEqualizer --version 1.0.0-beta2
  ```

## Description of utility commands

To display a list of commands, run
  ```shell
  dotnet ShardEqualizer --help
  ```

To display a list of command options, run
  ```shell
  dotnet ShardEqualizer [command] --help
  ```
here `[command]` is the name of the command 

## Initialize

Run command `config-init` to create standard configuration files for your sharded MongoDB cluster.

  ```shell
  dotnet ShardEqualizer config-init --config=myCluster.xml --hosts=localhost
  ```

## Creating a starting data distribution

  ```shell
  dotnet ShardEqualizer presplit --config=myCluster.xml
  ```

## Chunks to be moved

  ```shell
  dotnet ShardEqualizer balancer --config=myCluster.xml
  ```

## Data size reports on shards

  ```shell
  dotnet ShardEqualizer deviation --config=myCluster.xml
  ```

## Aligning

  ```shell
  dotnet ShardEqualizer equalize --config=myCluster.xml
  ```





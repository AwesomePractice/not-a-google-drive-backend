# not-a-google-drive-backend

# Swagger: http://34.105.195.56/swagger/index.html
## ASP.NET Core Docker containers startup

This sample demonstrates how to build container images for ASP.NET Core web apps. You can use these samples for Linux and Windows containers, for x64, ARM32 and ARM64 architectures.

The sample builds an application in a [.NET SDK container](https://hub.docker.com/_/microsoft-dotnet-sdk/) and then copies the build result into a new image (the one you are building) based on the smaller [.NET Docker Runtime image](https://hub.docker.com/_/microsoft-dotnet-runtime/). You can test the built image locally or deploy it to a [container registry](../push-image-to-acr.md).

The instructions assume that you have cloned this repo, have [Docker](https://www.docker.com/products/docker) installed, and have a command prompt open within the `samples/aspnetapp` directory within the repo.

## Try a pre-built version of the sample

If want to skip ahead, you can try a pre-built version with the following command and access it in your web browser at `http://localhost:8000`.

```console
docker run --rm -it -p 8000:80 mcr.microsoft.com/dotnet/samples:aspnetapp
```

## Build an ASP.NET Core image

You can build and run a .NET-based container image using the following instructions:

```console
docker build --pull -t aspnetapp .
docker run --rm -it -p 8000:80 aspnetapp
```

You should see the following console output as the application starts:

```console
> docker run --rm -it -p 8000:80 aspnetapp
Hosting environment: Production
Content root path: /app
Now listening on: http://[::]:80
Application started. Press Ctrl+C to shut down.
```

After the application starts, navigate to `http://localhost:8000` in your web browser.

> Note: The `-p` argument maps port 8000 on your local machine to port 80 in the container (the form of the port mapping is `host:container`). See the [Docker run reference](https://docs.docker.com/engine/reference/commandline/run/) for more information on command-line parameters. In some cases, you might see an error because the host port you select is already in use. Choose a different port in that case.

You can also view the ASP.NET Core site running in the container on another machine. This is particularly useful if you are wanting to view an application running on an ARM device like a Raspberry Pi on your network. In that scenario, you might view the site at a local IP address such as `http://192.168.1.18:8000`.

In production, you will typically start your container with `docker run -d`. This argument starts the container as a service, without any console interaction. You then interact with it through other Docker commands or APIs exposed by the containerized application.

We recommend that you do not use `--rm` in production. It cleans up container resources, preventing you from collecting logs that may have been captured in a container that has either stopped or crashed.

> Note: See [Establishing docker environment](../establishing-docker-environment.md) for more information on correctly configuring Dockerfiles and `docker build` commands.

## Build an image for Debian (Our case)

.NET multi-platform tags result in Debian-based images, for Linux. For example, you will pull a Debian-based image if you use a simple version-based tag, such as `5.0`, as opposed to a distro-specific tag like `5.0-alpine`.

This sample includes Dockerfile examples that explicitly target Alpine, Debian and Ubuntu. The [.NET Docker Sample](../dotnetapp/README.md) demonstrates targeting a larger set of distros.

The following example demonstrates targeting distros explicitly and also shows the size differences between the distros. Tags are added to the image name to differentiate the images.

On Linux:

```console
docker build --pull -t aspnetapp:alpine -f Dockerfile.alpine-x64 .
docker run --rm -it -p 8000:80 aspnetapp:alpine
```

You can view in the app in your browser in the same way as demonstrated earlier.

You can also build for Debian and Ubuntu:

```console
docker build --pull -t aspnetapp:debian -f Dockerfile.debian-x64 .
docker build --pull -t aspnetapp:ubuntu -f Dockerfile.ubuntu-x64 .
```

You can use `docker images` to see the images you've built and to compare file sizes:

```console
% docker images aspnetapp
REPOSITORY          TAG                 IMAGE ID            CREATED             SIZE
aspnetapp           ubuntu              0f5bc72e4caf        14 seconds ago      209MB
aspnetapp           debian              f70387d4d802        35 seconds ago      212MB
aspnetapp           alpine              6da2c287c42c        10 hours ago        109MB
aspnetapp           latest              8c5d1952e3b7        10 hours ago        212MB
```
## More Samples

* [.NET Docker Samples](../README.md)
* [.NET Framework Docker Samples](https://github.com/microsoft/dotnet-framework-docker/blob/main/samples/README.md)

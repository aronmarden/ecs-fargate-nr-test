# Use the .NET SDK base image for building the application (ARM64 version)
FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["ecs-fargate-netcore-testapp/ecs-fargate-netcore-testapp.csproj", "./"]
RUN dotnet restore "ecs-fargate-netcore-testapp.csproj"

# Copy everything else and build
COPY ecs-fargate-netcore-testapp/. ./
RUN dotnet publish "ecs-fargate-netcore-testapp.csproj" -c Release -o out

# Use the .NET runtime base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the New Relic .NET agent folder first
COPY ecs-fargate-netcore-testapp/newrelic_dotnet_agent_arm64 /usr/local/newrelic_dotnet_agent_arm64

# Copy the published output
COPY --from=build-env /app/out .

# Set the environment variable to listen on port 80
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
EXPOSE 443

# Install curl, jq, and New Relic agent
RUN apt-get update && \
    apt-get install -y curl jq wget ca-certificates gnupg

# Download and add the New Relic GPG key
RUN wget https://download.newrelic.com/548C16BF.gpg && apt-key add 548C16BF.gpg

# Define build arguments for New Relic license key and app name
ARG NEW_RELIC_LICENSE_KEY_ARG
ARG NEW_RELIC_APP_NAME_ARG

# Enable the New Relic agent
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic_dotnet_agent_arm64 \
    CORECLR_PROFILER_PATH=/usr/local/newrelic_dotnet_agent_arm64/extensions/libNewRelicProfiler.so \
    NEW_RELIC_LICENSE_KEY=${NEW_RELIC_LICENSE_KEY_ARG} \
    NEW_RELIC_APP_NAME="${NEW_RELIC_APP_NAME_ARG}" 

ENTRYPOINT ["dotnet", "ecs-fargate-netcore-testapp.dll"]

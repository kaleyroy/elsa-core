FROM mcr.microsoft.com/dotnet/core/sdk:3.1
MAINTAINER kaleyroy "kaleyroy@gmail.com"

LABEL image=elsa
ENV ASPNETCORE_URLS=http://*:5008

RUN apt-get update
RUN apt-get install -y wget vim
RUN apt-get install -y iproute2 iputils-ping net-tools

WORKDIR /app
COPY app/ /app

EXPOSE 5008
ENTRYPOINT ["dotnet", "Sample16.dll"]
FROM mcr.microsoft.com/dotnet/runtime:5.0.2

ENV DOTNET_EnableDiagnostics=0
COPY BumbleDocker/ App/
RUN mkdir -p /root/Desktop/
COPY Desktop/ /root/Desktop/
WORKDIR /App
ENTRYPOINT ["dotnet", "BumbleBot.dll"]

for %%R in (win-x86,win-x64,win-arm,win-arm64,osx-x64,linux-x64,linux-arm) do dotnet publish -c Release -r %%R

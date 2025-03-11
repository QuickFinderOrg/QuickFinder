dotnet publish -c Release -r linux-x64
wsl ssh -J sturlen@ssh4.ux.uis.no sturlen@gorina3.ux.uis.no 'rm -rf /local/home/quickfinder/publish'
wsl scp -r -J sturlen@ssh4.ux.uis.no /mnt/c/projects/group-finder/publish/ sturlen@gorina3.ux.uis.no:/local/home/quickfinder/
wsl scp -r -J sturlen@ssh4.ux.uis.no /mnt/c/projects/group-finder/appsettings.Production.json sturlen@gorina3.ux.uis.no:/local/home/quickfinder/publish
# wsl ssh -J sturlen@ssh4.ux.uis.no sturlen@gorina3.ux.uis.no "tmux new-session -d -s webserver -c /local/home/quickfinder/publish"
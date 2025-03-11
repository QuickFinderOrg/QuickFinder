ssh -J sturlen@ssh4.ux.uis.no sturlen@gorina3.ux.uis.no "tmux kill-session -t webserver"

ssh -J sturlen@ssh4.ux.uis.no sturlen@gorina3.ux.uis.no 'rm -rf /local/home/quickfinder/publish' &&
scp -r -J sturlen@ssh4.ux.uis.no /mnt/c/projects/group-finder/publish/ sturlen@gorina3.ux.uis.no:/local/home/quickfinder/ &&
scp -r -J sturlen@ssh4.ux.uis.no /mnt/c/projects/group-finder/appsettings.Production.json sturlen@gorina3.ux.uis.no:/local/home/quickfinder/publish

read -p "Do you want to start the webserver as well? (y/n): " webserver_answer
if [ "$webserver_answer" = "y" ]; then
    ssh -J sturlen@ssh4.ux.uis.no sturlen@gorina3.ux.uis.no "tmux new-session -d -s webserver 'cd /local/home/quickfinder/publish && dotnet group-finder.dll'"
fi

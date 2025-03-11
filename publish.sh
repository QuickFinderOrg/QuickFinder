#!/bin/bash
QUICKFINDER_DIR="/local/home/quickfinder/"
LOGIN_SERVER="sturlen@ssh4.ux.uis.no"
UNIX_SERVER="sturlen@gorina3.ux.uis.no"
PROJECT_DIR="/mnt/c/projects/group-finder/"

ssh -J $LOGIN_SERVER $UNIX_SERVER "tmux kill-session -t webserver"

ssh -J $LOGIN_SERVER $UNIX_SERVER 'rm -rf /local/home/quickfinder/publish' &&
scp -r -J sturlen@ssh4.ux.uis.no /mnt/c/projects/group-finder/publish/ sturlen@gorina3.ux.uis.no:/local/home/quickfinder/ &&
scp -r -J sturlen@ssh4.ux.uis.no /mnt/c/projects/group-finder/appsettings.Production.json sturlen@gorina3.ux.uis.no:/local/home/quickfinder/publish

read -p "Do you want to start the webserver as well? (y/n): " webserver_answer
if [ "$webserver_answer" = "y" ]; then
    ssh -J $LOGIN_SERVER $UNIX_SERVER "tmux new-session -d -s webserver 'cd /local/home/quickfinder/publish && dotnet group-finder.dll'"
fi

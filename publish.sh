#!/bin/bash
QUICKFINDER_DIR="/local/home/quickfinder"
LOGIN_SERVER="sturlen@ssh4.ux.uis.no"
UNIX_SERVER="sturlen@gorina3.ux.uis.no"
PROJECT_DIR="/mnt/c/projects/group-finder"

stop-server() {
    ssh -J $LOGIN_SERVER $UNIX_SERVER "tmux kill-session -t webserver" | tee /dev/tty
}

start-server() {
    ssh -J $LOGIN_SERVER $UNIX_SERVER "tmux new-session -d -s webserver 'cd $QUICKFINDER_DIR/publish && dotnet group-finder.dll'"
}

stop-server

# copy ENV
cp appsettings.Production.json publish/
# remove existing project and upload new
ssh -J $LOGIN_SERVER $UNIX_SERVER 'rm -rf /local/home/quickfinder/publish' &&
tar -czf - publish/ | ssh -J $LOGIN_SERVER $UNIX_SERVER "tar -xzf - -C /local/home/quickfinder/"

read -p "Do you want to start the webserver as well? (y/n): " webserver_answer
if [ "$webserver_answer" = "y" ]; then
    start-server
fi

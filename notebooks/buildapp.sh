#!/bin/bash
set -eux

mkdir -p /home/azureuser/app
chown -R azureuser:azureuser /home/azureuser
sudo -u azureuser sh -c 'cd /home/azureuser/app && dotnet new mvc'

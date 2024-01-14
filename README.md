# SimpleFileUpdater
 This is a simple python file server and dotnet client to check md5 against server files and download if different

# Building
Clone the repo  
Run in terminal/command prompt/powershell etc: `dotnet build -c Release`  

# Server
On your server make sure you have python 3.11+ installed  
Simply run python serv.py, it will create a `files/` folder where you will place all files you want the client to be able to check/download.  
When you update files, delete `jsoncache.json` so the server will generate a new one(it may take a minute depending on how large/quantity of files)  

# Client
After building the project, place the output files in the `same` directory you'd like the server files to be downloaded to.  
Run `FileUpdaterClient.exe`  
This will check your files md5 vs the server's md5 and download any differing or non-existant files.  

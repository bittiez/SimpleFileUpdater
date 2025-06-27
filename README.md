# SimpleFileUpdater
This is a simple python file server and dotnet client to check md5 against server files and download if different.  
- This is intended for servers to provide an easy way for players to stay up to date with their files, only downloading files the player needs, instead of all of them in a zip.
- This is customizable to your needs; you can change art, text, etc.

![image](https://github.com/user-attachments/assets/19862851-c269-4c8e-a442-71060d5fbc2b)  
![image](https://github.com/user-attachments/assets/ef2e7725-02b8-498d-a93d-449ce535e062)  


## Building and Customizing the client
1. Clone the repo  
2. Change `resources/background.png` to your own background. 800x450 in size.
3. Change `resources/icon.co` to your own icon. Size isn't too important.
4. Open `Settings.cs` and update with your info.
5. Run in terminal/command prompt/powershell etc: `dotnet build -c Release`  

### Build information
- This is cross platform, to build on other platforms use:
- Linux: `dotnet build -c Release -r linux-x64`
- MacOS: `dotnet build -c Release -r osx-x64` *May need to be ran on a Mac*
- Windows: `dotnet build -c Release -r win-x64`
- Each platform needs it's own release.

# Server
1. On your server make sure you have python 3.11+ installed  
2. Run `python3 serv.py`
3. This will create a `files/` folder where you will place all files you want the client to be able to check/download.  
4. After placing your files in there, restart the server.
4. When you update files, either restart the server or wait one hour. File cache is regenerated every hour. 

# Client Info
- After building the project, place the output files in the `same` directory you'd like the server files to be downloaded to.  
- - For example:
```
/FileUpdaterClient.exe
/map0.mul
/map1.mul
```
- Run `FileUpdaterClient.exe`  
- This will check your files md5 vs the server's md5 and download any differing or non-existent files.  

import os
from http.server import BaseHTTPRequestHandler, HTTPServer
import json
import hashlib
from pathlib import Path

PORT = 8080
HOSTNAME = ""
FILESDIR = './files/'

class FServer(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith("/file/"):
            file = FILESDIR + self.path[6:]
            if Path(file).is_file():
                self.send_response(200)
                self.send_header("Content-type", "application/octet-stream")
                self.end_headers()
                with open(file, 'rb') as fileh:
                    self.wfile.write(fileh.read())
                return
            else:
                self.send_error(404)
                return

        self.send_response(200)
        self.send_header("Content-type", "application/json")
        self.end_headers()
        self.wfile.write(self.getJson())
        
    def getJson(self):
        jsonCache = Path("jsoncache.json")
        
        if jsonCache.is_file():
            return open("jsoncache.json",'rb').read()
    
        data = []
        
        c = 0;
        allFiles = self.fileList(FILESDIR)
        for file in allFiles:
            if os.path.isfile(file):
                tdata = {}
                tdata['name'] = file[len(FILESDIR):]
                
                md5 = hashlib.md5(open(file,'rb').read()).hexdigest()
                
                tdata['md5'] = md5
                data.append(tdata)
                c = c + 1
        
        result = json.dumps(data)
        
        f = open("jsoncache.json", "w")
        f.write(result)
        f.close()
        
        return bytes(result, "utf-8")
        
    def fileList(self, where):
        matches = []
        for root, dirnames, filenames in os.walk(where):
            for filename in filenames:
                    matches.append(os.path.join(root, filename))
        return matches

if __name__ == "__main__":      
    os.makedirs(FILESDIR, exist_ok=True)
    
    webServer = HTTPServer((HOSTNAME, PORT), FServer)
    print("Server started http://%s:%s" % (HOSTNAME, PORT))
    print()
    print("Delete jsoncache.json when files have changed.")

    try:
        webServer.serve_forever()
    except KeyboardInterrupt:
        pass

    webServer.server_close()
    print("Server stopped.")
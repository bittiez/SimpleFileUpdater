#!/bin/bash

while true; do
    python3 serv.py &
    PID=$!
    # Wait for 6 hours (21600 seconds)
    sleep 21600
    echo "Restarting server to keep it fresh."
    kill -9 $PID 2>/dev/null
    wait $PID 2>/dev/null
done
#!/bin/bash

# Update Unity Hub projectsInfo.json to include the desired CLI argument
CONFIG_FILE="$HOME/.config/unityhub/projects-v1.json"
PROJECT_PATH="$(pwd)"

# Check if jq is installed, install if not
if ! command -v jq &> /dev/null; then
    echo "jq not found. Installing..."
    sudo apt-get update && sudo apt-get install -y jq
fi

if [ -f "$CONFIG_FILE" ]; then
    # Use jq to append the CLI argument to the JSON file
    jq --arg path "$PROJECT_PATH" '.data[$path].path = $path ' "$CONFIG_FILE" > "$CONFIG_FILE.tmp" && mv "$CONFIG_FILE.tmp" "$CONFIG_FILE"
    echo "Updated $CONFIG_FILE with --force-vulkan argument."
else
    echo "Error: $CONFIG_FILE does not exist."
fi

killall unityhub-bin 
echo "Restarted Unity Hub to apply changes."
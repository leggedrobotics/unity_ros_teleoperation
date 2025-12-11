#!/bin/bash

# Update Unity Hub projectsInfo.json to include the desired CLI argument
CONFIG_FILE="$HOME/.config/unityhub/projectsInfo.json"
PROJECT_PATH="$(pwd)"

if [ -f "$CONFIG_FILE" ]; then
    # Use jq to append the CLI argument to the JSON file
    jq --arg v "$PROJECT_PATH" '.[$v].cliArgs |= ( " --force-vulkan")' "$CONFIG_FILE" > "$CONFIG_FILE.tmp" && mv "$CONFIG_FILE.tmp" "$CONFIG_FILE"
    echo "Updated $CONFIG_FILE with --force-vulkan argument."
else
    echo "Error: $CONFIG_FILE does not exist."
fi

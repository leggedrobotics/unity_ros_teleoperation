#!/bin/bash

# Update Unity Hub projectsInfo.json to include the desired CLI argument
CONFIG_FILE="$HOME/.config/unityhub/projects-v1.json"
CLI_FILE="$HOME/.config/unityhub/projectsInfo.json"

# Install Git hooks
HOOKS_DIR="$(pwd)/scripts/git-hooks"
GIT_HOOKS_DIR="$(git rev-parse --git-dir)/hooks"

echo " Installing Git hooks..."

for hook in "$HOOKS_DIR"/*; do
    hook_name=$(basename "$hook")
    echo "Installing $hook_name"
    ln -sf "$hook" "$GIT_HOOKS_DIR/$hook_name"
    chmod +x "$hook"
done

echo "Git hooks installed!"

echo "Configuring Unity Hub..."
PROJECT_PATH="$(pwd)"

# Check if jq is installed, install if not
if ! command -v jq &> /dev/null; then
    echo "jq not found. Installing..."
    sudo apt-get update && sudo apt-get install -y jq
fi

# Ensure the configuration files exist, create them with empty JSON if they don't
if [ ! -f "$CONFIG_FILE" ]; then
    echo "{"schema_version":"v1","data":{}" > "$CONFIG_FILE"
    echo "Created empty $CONFIG_FILE."
fi

if [ ! -f "$CLI_FILE" ]; then
    echo "{}" > "$CLI_FILE"
    echo "Created empty $CLI_FILE."
fi

# Use jq to append the CLI argument to the CLI config file, and add the project to the new UnityHub config file
jq --arg path "$PROJECT_PATH" '.data[$path].path = $path ' "$CLI_FILE" > "$CLI_FILE.tmp" && mv "$CLI_FILE.tmp" "$CLI_FILE"
jq --arg v "$PROJECT_PATH" '.[$v].cliArgs |= ( " --force-vulkan")' "$CONFIG_FILE" > "$CONFIG_FILE.tmp" && mv "$CONFIG_FILE.tmp" "$CONFIG_FILE"


echo "Updated $CONFIG_FILE and $CLI_FILE with $PROJECT_PATH."

killall unityhub-bin 
echo "Restarted Unity Hub to apply changes."

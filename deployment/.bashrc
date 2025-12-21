export SSH_AUTH_SOCK="$HOME/.ssh/agent.sock"

# Start agent if not reachable
if ! ssh-add -l >/dev/null 2>&1; then
  rm -f "$SSH_AUTH_SOCK"
  eval "$(ssh-agent -a "$SSH_AUTH_SOCK" -s)" >/dev/null
fi

# Load your key (no prompt if no passphrase)
ssh-add -q "$HOME/.ssh/pi0_github_ed25519" 2>/dev/null
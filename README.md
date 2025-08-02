// 🔮 INIT CLIENTS — Discord & BadgeClient
// 📜 LOAD BADGE CONFIG — badge-locations.yml
// 🧿 LISTEN FOR SPONSOR PING — Trigger dropBadge()
- name: Confirm README presence
  run: |
    if [[ ! -f README.md ]]; then
      echo "ERROR: README.md not found in workspace!"
      exit 1
    fi
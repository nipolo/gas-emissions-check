alias cls=clear
alias edith="sudo nano /etc/hosts"
alias edita='nano ~/.bash_aliases'
alias reloada='source ~/.bash_aliases'

alias gf='git fetch'
alias gs='git status'
alias gst='git stash'
alias gstp='git stash pop'
alias gco='git checkout origin/$1'
alias gcb='git checkout -b $1'
alias gcl='git clone $1'
alias gc='git checkout $1'
alias gcd='git checkout development'
alias gcm='git checkout master'
alias gpl='git pull'
alias gps='git push'
alias gundo='git restore $1'

alias redeploy="$HOME/scripts/deploy_sensor.sh"
alias gec_logs="journalctl -u gasemissionscheck-sensor.service -f"
alias gec_start="sudo systemctl start gasemissionscheck-sensor.service && journalctl -u gasemissionscheck-sensor.service -f"
alias gec_stop="sudo systemctl stop gasemissionscheck-sensor.service"
alias gec_disable="sudo systemctl disable gasemissionscheck-sensor.service"
alias gec_status="systemctl status gasemissionscheck-sensor.service --no-pager"
alias gec_edit="sudo nano /etc/systemd/system/gasemissionscheck-sensor.service"
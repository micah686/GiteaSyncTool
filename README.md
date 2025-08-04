# Gitea Sync Tool

Quick tool to pull down your repos and stars from Github, and push them to Gitea.
Have a ~2.5 second delay between imports in order not to hit a throttle/rate limit of the github API
Access tokens are encrypted in the config, so if you see a `GithubToken_ENC_01234567890123456789`, the encrypted value will only be read at runtime.

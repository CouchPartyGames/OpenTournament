# OpenTournament

[![Basic validation](https://github.com/CouchPartyGames/OpenTournament/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/CouchPartyGames/OpenTournament/actions/workflows/dotnet.yml)


### Docker and Docker Compose

Create an `.env` file in `extra` directory.
```
POSTGRES_PASSWORD=YourPassword
POSTGRES_USER=YourUser
POSTGRES_DB=tournament
REDIS_PASSWORD=YourPassword
```

`extra/docker-compose.yaml`

```
docker compose up
docker compose down
```

### SystemD Configuration
You can find an example service file in `extra/opentournament.service`.
It's recommended to install in /etc/systemd/system directory.

```
systemctl daemon-reload
systemctl enable --now opentournament.service
```
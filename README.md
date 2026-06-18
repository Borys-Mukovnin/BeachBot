# What this project is?

Automation app for registering for beach volleyball tournaments on https://www.beachvolleyball.de/. It logs in, checks the tournament list, and schedules a registration request for your team at the right time.

It should have a clean structure and code (no unnecessary or over-complex design patterns).

## Project description

Activity diagramm + explanations is at "BeachBot_Activity_Diagramm.drawio"

The backend should be decopled from the frontend and developed first. It should be possible to
connect later other frontend platforms to the same backend API.

## Protocol

The site is a **Meteor** app: the backend speaks **DDP over a raw WebSocket** (`/websocket`), *not* HTTP/REST. So the "API client" is a WebSocket client — there is no `HttpClient` and there are no cookies.

- **One call = one message.** Request `{"msg":"method","method":<name>,"params":[...],"id":<id>}`; reply `{"msg":"result","id":<id>,"result":...}` or `{...,"error":{...}}`, matched back up by `id`. A `connect` handshake is sent once after the socket opens.
- **Auth is token-based.** The `login` method takes the email + a **client-side SHA-256** password digest and returns a **resume token** + expiry. Later sessions re-authenticate with that token instead of the password.
- **EJSON timestamps:** every date is `{"$date": <unixMillis>}`; ids are 17-char Meteor strings.

Full request/response shape for every method: `api_schema.json`.

## Structure

```
src/
  ├── BeachBot.Core/            classlib   ← domain, zero external deps
  │     Models/                 Tournament, Player, Team, Registration
  │     Abstractions/           ITournamentApi, IRegistrationService, IClock
  │
  ├── BeachBot.Api/             classlib   ← beachvolleyball.de DDP/WebSocket client
  │     Ddp/                    DDP-over-WebSocket transport (Meteor protocol)
  │       IDdpConnection.cs       seam: CallAsync<T>(method, params) → typed result
  │       DdpConnection.cs        ClientWebSocket: connect handshake + id↔result correlation
  │       DdpException.cs         typed wrapper around a DDP `error` reply
  │     Auth/                   login / token (Meteor resume token — no cookies)
  │       PasswordHasher.cs       plaintext password → SHA-256 hex digest
  │       AuthToken.cs            resume token + expiry value object
  │       IAuthenticator.cs       LoginAsync / ResumeAsync
  │       Authenticator.cs        runs the `login` method, maps reply → AuthToken
  │     Dtos/                   raw JSON shapes (login, tournament, team, player; EJSON $date)
  │     Mapping/                DTO → Core
  │     TournamentApiClient.cs  typed facade over IDdpConnection, implements Core.ITournamentApi
  │
  ├── BeachBot.Application/     classlib   ← use-cases / orchestration
  │     RegistrationService.cs
  │     RegistrationScheduler.cs  fires at RegistrationStart
  │     PlayerProfileStore.cs     your saved players/teams (local JSON/SQLite)
  │
  └── BeachBot.Desktop/  (your "frontend")  WPF or Avalonia, Exe ← the only runnable project
        App.xaml / Program
        Views/                  TournamentsView, TeamsView, StatusView
        ViewModels/             MVVM, bind to Application services
        DI/                     ServiceCollection wiring

tests/
```


## UI Guidelines

UI should be in dark/blue colors, clean and simple, however functional
and comfortable.
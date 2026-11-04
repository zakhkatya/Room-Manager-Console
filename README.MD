# Room Manager Console

A C# console application for managing room reservations around a university campus. The application allows users to manage rooms and organize reservations through an interactive terminal interface.

## Features

- **Room Management** – Add, view, update, and delete rooms.
- **Reservation Management** – Create, view, update, and delete room reservations.
- **Room Availability** – Check room availability and avoid scheduling conflicts.
- **Reservation Details** – Store event names, reserved names, dates, times, and additional notes.
- **SQLite Database** – Persist room and reservation data locally.
- **Interactive Console UI** – Navigate the application through a terminal interface.

## Technologies

- C# / .NET 10
- SQLite
- Microsoft.Data.Sqlite
- Spectre.Console

## Project Structure

```
Room-Manager-Console/
│
├── Controllers/
│   ├── AppController.cs
│   ├── RoomController.cs
│   └── ReservationController.cs
│
├── Models/
│   ├── Room.cs
│   ├── RoomModel.cs
│   ├── Reservation.cs
│   └── ReservationModel.cs
│
├── Views/
│   └── ConsoleView.cs
│
├── Program.cs
├── pr1.csproj
└── README.md
```

The project follows the **Model-View-Controller (MVC)** architectural pattern:

- **Models** – Handle application data and database operations.
- **Views** – Display information and interact with the user.
- **Controllers** – Handle application logic and coordinate communication between models and views.

## Requirements


- .NET 10 SDK
- Git

## Installation and Setup

```bash
git clone https://github.com/zakhkatya/Room-Manager-Console.git

cd Room-Manager-Console

dotnet restore

dotnet run
```

The application initializes the SQLite database tables automatically when it starts.

## Database

The application uses SQLite for local data storage.
It maintains two main entities:

**Room**
- ID
- Room name
- Capacity

**Reservation**
- ID
- Start date and time
- End date and time
- Event name
- Reserved by
- Additional notes
- Associated room

## Purpose

This project was developed as part of a university assignment focused on implementing an interactive user interface in a console environment using C#.
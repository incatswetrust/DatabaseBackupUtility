# Database Backup Utility
![Database Backup Utility Logo](logo.png)


## Overview

The **Database Backup Utility v.2.0.0** is a versatile tool designed to simplify the process of creating and restoring backups for various database management systems (DBMS). This tool is especially useful for developers and administrators who need a reliable way to manage database backups via a command-line interface.

## Features

* **Multi-DBMS Support:** Supports MySQL, PostgreSQL, and MongoDB out of the box.
* **Automated Backups:** Easily set up automated backups with configurable options.
* **Command-Line Interface:** Manage backups and restores using simple command-line commands.
* **Flexible Storage Options:** Store backups locally or in cloud storage solutions.
* **Compression:** Optionally gzip backups to save space.
* **Notifications:** Receive notifications (e.g., via Slack) upon completion of backup or restore operations.
* **Modular Architecture:** Easily extend the tool to support additional DBMS or custom storage solutions.

## Supported Databases

* **MySQL:** One of the most popular relational databases, commonly used in web applications.
* **PostgreSQL:** A robust, open-source object-relational database known for its reliability and advanced features.
* **MongoDB:** A NoSQL database oriented towards document storage, widely used for handling large volumes of unstructured data.

## Requirements

* [.NET 8 runtime](https://dotnet.microsoft.com/download) to run the utility.
* The command-line client for whichever database you back up, available on `PATH`:
  * MySQL: `mysqldump` and `mysql`
  * PostgreSQL: `pg_dump` and `psql`
  * MongoDB: `mongodump` and `mongorestore`

If a required tool isn't found on `PATH`, backup/restore commands fail with a message naming the missing tool.

## Installation

1. Download the Executable: Download the latest release of `DatabaseBackupUtility.exe` from the Releases page.
2. Prepare Configuration File:
* Create a `config.json` file in the same directory as the executable (see [Configuration](#configuration) below for a full example per database type). A template with placeholder values is also checked in at `DatabaseBackupUtility/Configs/appsettings.example.json`.
3. Run the Tool:
* Open a command-line interface and navigate to the directory containing `DatabaseBackupUtility.exe`.
* Execute commands to perform backups or restores.

## Usage

The command comes first, followed by options:

```bash
DatabaseBackupUtility <command> --config <path_to_config> [options]
```

### Commands

| Command | Description |
| --- | --- |
| `backup` | Create a backup of the configured database. |
| `restore` | Restore the database from a backup. |
| `test-connection` | Check connectivity to the configured database without doing anything else. |
| `list` | List existing backups for the configured database (Local storage only). |

### Backup

```bash
DatabaseBackupUtility backup --config config.json
```

Options:
* `--output <path>` — write the backup to an exact path instead of the auto-generated, timestamped name under `Storage.LocalPath`.
* `--compress` — gzip the backup file (`.sql.gz`) after it's created.
* `--dry-run` — validate configuration and test the database connection without actually creating a backup.

### Restore

```bash
DatabaseBackupUtility restore --config config.json
```

Options:
* `--file <name>` — restore from a specific backup file instead of the most recent one for the configured database. Compressed (`.gz`) backups are decompressed automatically.
* `--dry-run` — validate configuration and test the database connection without actually restoring.

### Test Connection

```bash
DatabaseBackupUtility test-connection --config config.json
```

### List Backups

```bash
DatabaseBackupUtility list --config config.json
```

Lists backup files for the configured database found in `Storage.LocalPath`, newest first. Only supported when `Storage.Type` is `Local`.

## Configuration

`config.json` has four top-level sections: `Database`, `Storage`, `Logging`, and (optionally) `Notifications`.

* **Database:** database type, connection details, and credentials.
* **Storage:** where backups are stored — locally or in the cloud.
* **Logging:** where the utility writes its own log file.
* **Notifications:** optional; when present, sends a Slack message on completion or failure of an operation.

`Database.Port` is optional — omit it to use each database's default port, or set it (`1`-`65535`) to target a non-standard port.

### MySQL example

```json
{
  "Database": {
    "Type": "MySql",
    "Host": "localhost",
    "Port": 3306,
    "DatabaseName": "mydatabase",
    "Username": "root",
    "Password": "password"
  },
  "Storage": {
    "Type": "Local",
    "LocalPath": "C:/Backups"
  },
  "Logging": {
    "Path": "logs/log.txt"
  },
  "Notifications": {
    "SlackWebhookUrl": "https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXXXXXX"
  }
}
```

### PostgreSQL example

```json
{
  "Database": {
    "Type": "PostgreSql",
    "Host": "localhost",
    "Port": 5432,
    "DatabaseName": "mydatabase",
    "Username": "postgres",
    "Password": "password"
  },
  "Storage": {
    "Type": "Local",
    "LocalPath": "C:/Backups"
  },
  "Logging": {
    "Path": "logs/log.txt"
  }
}
```

### MongoDB example

```json
{
  "Database": {
    "Type": "MongoDb",
    "Host": "localhost",
    "Port": 27017,
    "DatabaseName": "mydatabase",
    "Username": "root",
    "Password": "password"
  },
  "Storage": {
    "Type": "S3",
    "Cloud": {
      "Provider": "AWS",
      "BucketName": "my-backup-bucket"
    }
  },
  "Logging": {
    "Path": "logs/log.txt"
  }
}
```

Never commit a `config.json`/`appsettings.json` with real credentials — `.gitignore` excludes `appsettings*.json` (aside from the checked-in `appsettings.example.json` template) for this reason.

## License

This project is licensed under the [MIT License](LICENSE).

https://roadmap.sh/projects/database-backup-utility

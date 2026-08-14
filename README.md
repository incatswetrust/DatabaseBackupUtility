# Database Backup Utility
![Database Backup Utility Logo](logo.png)


## Overview

The **Database Backup Utility v.2.0.0** is a versatile tool designed to simplify the process of creating and restoring backups for various database management systems (DBMS). This tool is especially useful for developers and administrators who need a reliable way to manage database backups via a command-line interface.

## Features

* **Multi-DBMS Support:** Supports MySQL, PostgreSQL, MongoDB, and SQLite out of the box.
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
* **SQLite:** A lightweight, file-based relational database that needs no separate server process.

## Requirements

* [.NET 8 runtime](https://dotnet.microsoft.com/download) to run the utility.
* The command-line client for whichever database you back up, available on `PATH`:
  * MySQL: `mysqldump` and `mysql`
  * PostgreSQL: `pg_dump` and `psql`
  * MongoDB: `mongodump` and `mongorestore`
  * SQLite: none — backup/restore is done through the bundled `Microsoft.Data.Sqlite` library, no `sqlite3` CLI required.

If a required tool isn't found on `PATH`, backup/restore commands fail with a message naming the missing tool.

## Installation

1. Download the Executable: Download the latest release of `DatabaseBackupUtility.exe` from the Releases page.
2. Prepare Configuration File:
* Create a `config.json` file in the same directory as the executable (see [Configuration](#configuration) below for a full example per database type). Templates with placeholder values are also checked in at `DatabaseBackupUtility/Configs/appsettings.example.json` (MySQL) and `DatabaseBackupUtility/Configs/appsettings.sqlite.example.json` (SQLite).
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
* `--type <full|incremental|differential>` — the kind of backup to create (default `full`). See [Backup types](#backup-types).
* `--output <path>` — write the backup to an exact path instead of the auto-generated, timestamped name under `Storage.LocalPath`.
* `--compress` — gzip the backup file (`.sql.gz`) after it's created.
* `--dry-run` — validate configuration and test the database connection without actually creating a backup.

### Restore

```bash
DatabaseBackupUtility restore --config config.json
```

Options:
* `--file <name>` — restore from a specific backup file instead of the most recent one for the configured database. Compressed (`.gz`) backups are decompressed automatically.
* `--table <name>` — restore only this table (MySQL/PostgreSQL). See [Selective restore](#selective-restore).
* `--collection <name>` — restore only this collection (MongoDB). See [Selective restore](#selective-restore).
* `--dry-run` — validate configuration and test the database connection without actually restoring.

Restore automatically resolves the full/incremental/differential chain the target backup belongs to (see [Backup types](#backup-types)) and applies every step in order. Backups taken before chain metadata existed, or files not tracked in it, fall back to a plain single-file full restore.

## Selective restore

By default, `restore` restores everything in the backup. To restore just one table or collection instead:

```bash
DatabaseBackupUtility restore --config config.json --table orders
DatabaseBackupUtility restore --config config.json --collection orders
```

* **MongoDB** (`--collection`) — points `mongorestore --collection` directly at that collection's BSON file inside the dump, so only it is touched.
* **MySQL/PostgreSQL** (`--table`) — since a full backup is one dump file covering every table, the utility filters it down to only the statements for the requested table (its `CREATE`/`DROP`, and its `INSERT`/`COPY` data) before applying it, leaving other tables untouched. This is a best-effort, line-oriented filter rather than a full SQL parser — fine for the common case of restoring one table's data, but a table name that only appears incidentally (e.g. as a foreign key reference inside another table's definition) could be pulled in too.

For MySQL/PostgreSQL, `--table` is applied to every step of an incremental/differential chain restore, since each step is itself a plain-SQL file the same filter works on. For MongoDB, `--collection` only restricts the initial full restore step — an incremental/differential chain's oplog replay still applies to the whole database, since oplog entries aren't filterable by collection the same way.

## Backup types

Every database except SQLite (a single-file snapshot has nothing to be incremental against) supports three backup types:

* **`full`** — a complete, standalone snapshot (`mysqldump`/`pg_dump`/`mongodump`), exactly as before. Every chain starts with one.
* **`incremental`** — changes since the most recent backup of *any* type. Restoring an incremental backup applies the full backup plus every incremental step since it, in order.
* **`differential`** — changes since the most recent *full* backup, regardless of how many differentials or incrementals came in between. Restoring a differential backup applies only the full backup plus that one differential.

Each backup's place in the chain is recorded in a small JSON sidecar file kept locally under `Storage.LocalPath` (even when the backup blob itself goes to cloud storage, since remote storage backends aren't listable). `restore` reads these sidecars to work out which files to download and apply, and in what order.

Per-engine mechanics:

* **MySQL** — incremental/differential backups are captured with `mysqlbinlog --read-from-remote-server`, streaming binary log events since the parent's `file:position` checkpoint over the network (no filesystem access to the server needed). The output is plain SQL, applied the same way as a full dump. Requires binary logging enabled and `REPLICATION SLAVE`/`REPLICATION CLIENT` privileges for the configured user.
* **PostgreSQL** — each full backup opens a pair of logical replication slots (`wal_level = logical` and the `wal2json` output plugin are required). Incremental backups consume one slot (so each call only sees changes since the last backup), while differential backups peek the other slot without consuming it (so every call sees everything since the full backup). The captured wal2json changes are translated into plain `INSERT`/`UPDATE`/`DELETE` statements and applied via `psql`, same as a full dump. Update/delete capture needs a primary key or `REPLICA IDENTITY FULL` on the affected tables; exotic column types (arrays, composite types, `bytea`) may need manual review.
* **MongoDB** — requires a replica set (a standalone `mongod` has no oplog). Incremental/differential backups dump `local.oplog.rs` entries newer than the parent's timestamp and are applied with `mongorestore --oplogReplay`. A chain with several incremental/differential steps has its oplog dumps merged into one before replay, since `mongorestore` only replays a single oplog per call.

```bash
DatabaseBackupUtility backup --config config.json --type full
DatabaseBackupUtility backup --config config.json --type incremental
DatabaseBackupUtility backup --config config.json --type differential
DatabaseBackupUtility restore --config config.json
```

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

### SQLite example

SQLite has no host, port, username, or password — `Database.FilePath` points at the `.db`/`.sqlite` file instead.

```json
{
  "Database": {
    "Type": "Sqlite",
    "FilePath": "C:/Data/mydatabase.db",
    "DatabaseName": "mydatabase"
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

Backups are taken with `VACUUM INTO`, which produces a consistent snapshot of the database file even while other connections are active against it. Restoring copies the backup file back over `Database.FilePath`.

Never commit a `config.json`/`appsettings.json` with real credentials — `.gitignore` excludes `appsettings*.json` (aside from the checked-in `appsettings.example.json` template) for this reason.

## License

This project is licensed under the [MIT License](LICENSE).

https://roadmap.sh/projects/database-backup-utility
